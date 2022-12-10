using Microsoft.AspNetCore.Mvc;
using NLog;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using WhatSearch.Core;
using WhatSearch.DataProviders.Interfaces;
using WhatSearch.Models;
using WhatSearch.Services.Interfaces;
using WhatSearch.Utility;
using static WhatSearch.Controllers.LineModels;

namespace WhatSearch.Controllers
{
    public class AuthController : Controller
    {
        static SystemConfig config = Ioc.GetConfig();
        static ILogger logger = LogManager.GetCurrentClassLogger();
        const string authorizeUrl = "https://access.line.me/oauth2/v2.1/authorize";
        [Route("linelogin")]
        [HttpGet]
        public IActionResult LoginByLine(string returnUrl)
        {
            string clientId = config.Line.ClientId;
            string clientSecret = config.Line.ClientSecret;
            string redirectUrl = config.Line.Callback;
            //string state = "0000";
            string state = returnUrl == null ? "" : Uri.EscapeDataString(returnUrl);
            string authUrl = new LineLoginClient(clientId, clientSecret, redirectUrl).GetAuthUrl(state);
            return Redirect(authUrl);
        }

        [Route("linecallback")]
        [HttpGet]
        public IActionResult LineCallback(string code, string state, string error)
        {
            LineUser lineUser;
            try
            {
                string clientId = config.Line.ClientId;
                string clientSecret = config.Line.ClientSecret;
                string redirectUrl = config.Line.Callback;

                LineLoginClient lineMgr = new LineLoginClient(clientId, clientSecret, redirectUrl);
                IUserService userSrv = Ioc.Get<IUserService>();
                TokenResponse tokenData = lineMgr.GetToken(code).Result;
                if (tokenData == null)
                {
                    return NotFound();
                }
                lineUser = lineMgr.GetLineUser(tokenData.AccessToken).Result;
                if (lineUser == null || string.IsNullOrEmpty(lineUser.UserId))
                {
                    return Content("Error to login by Line.");
                }
                Member mem = userSrv.GetMember(lineUser.UserId);
                string accessToken;
                if (mem == null)
                {
                    mem = new Member
                    {
                        Name = lineUser.UserId,
                        DisplayName = lineUser.DisplayName,
                        Picture = lineUser.PictureUrl,
                        Status = MemberStatus.Invalice,
                    };
                    bool success = userSrv.SaveMember(mem, out accessToken);
                }
                else
                {
                    accessToken = mem.AccessToken;
                    userSrv.UpdateMember(mem.Name);
                }
                if (mem.Status == MemberStatus.Invalice)
                {
                    return Content("你沒有通過認證，請在Line上跟 unicorn 說一下。");
                }
                if (string.IsNullOrEmpty(accessToken) == false)
                {
                    userSrv.ForceLogin(Response, accessToken, config.Login.CookieDays);
                }

                //line private
                var friendshipStatus = lineMgr.GetFriendshipStatus(tokenData.AccessToken).Result;

                if (string.IsNullOrEmpty(state) == false)
                {
                    return Redirect(state);
                }
            } 
            catch (Exception ex)
            {
                logger.Error("Line callback失敗." , ex);
                throw;
            }
            return Content("Line: " + JsonHelper.Serialize(lineUser));
            //return Content("Line User: " + profile.UserId + " / " + friendshipStatus.FriendFlag);
        }
    }

    public class LineLoginClient
    {
        private HttpClient httpClient;

        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string RedirectUri { get; set; }

        public LineLoginClient(string clientId, string clientSecret, string redirectUri)
        {
            httpClient = new HttpClient();
            this.ClientId = clientId;
            this.ClientSecret = clientSecret;
            this.RedirectUri = redirectUri;
        }

        public string GetAuthUrl(string state)
        {
            string authorizeUrl = "https://access.line.me/oauth2/v2.1/authorize";
            //string scopes = Uri.EscapeDataString("openid profile email");
            string scopes = Uri.EscapeDataString("profile");
            var authUrl = string.Format(
                "{0}?response_type=code&client_id={1}&redirect_uri={2}&state={3}&scope={4}&promp=consent&bot_prompt=normal",
                authorizeUrl, ClientId, RedirectUri, state, scopes);
            return authUrl;
        }

        public async Task<TokenResponse> GetToken(string code)
        {
            var content = new List<KeyValuePair<string, string>>();
            content.Add(new KeyValuePair<string, string>("grant_type", "authorization_code"));
            content.Add(new KeyValuePair<string, string>("code", code));
            content.Add(new KeyValuePair<string, string>("redirect_uri", RedirectUri));
            content.Add(new KeyValuePair<string, string>("client_id", ClientId));
            content.Add(new KeyValuePair<string, string>("client_secret", ClientSecret));
            var response = httpClient.PostAsync("https://api.line.me/oauth2/v2.1/token",
                new FormUrlEncodedContent(content)).Result;
            var tokenResponse = JsonHelper.Deserialize<TokenResponse>(await response.Content.ReadAsStringAsync());
            if (!string.IsNullOrEmpty(tokenResponse.IdToken))
                tokenResponse.JWTPayload = GetJWTFromIdToken(tokenResponse.IdToken);

            return tokenResponse;         
        }

        private JWTPayload GetJWTFromIdToken(string idToken)
        {
            var tokens = idToken.Split('.');
            var header = Encoding.UTF8.GetString(Convert.FromBase64String(GetBase64URLCompatString(tokens[0])));

            var payload = Encoding.UTF8.GetString(Convert.FromBase64String(GetBase64URLCompatString(tokens[1])));
            var signature = Convert.FromBase64String(GetBase64URLCompatString(tokens[2]));
            if (VerifySignature(signature, string.Join(".", tokens[0], tokens[1])))
                return JsonHelper.Deserialize<JWTPayload>(payload);
            else
                throw new Exception("invalid signature");
        }

        private string GetBase64URLCompatString(string content)
        {
            var padding = (4 - content.Length % 4);
            if (padding == 4)
                return content;
            else
                return (content + new string('=', padding)).Replace('-', '+').Replace('_', '/');
        }

        private bool VerifySignature(byte[] signature, string content)
        {
            try
            {
                var key = Encoding.UTF8.GetBytes(ClientSecret);
                var body = Encoding.UTF8.GetBytes(content);

                using (HMACSHA256 hmac = new HMACSHA256(key))
                {
                    var hash = hmac.ComputeHash(body, 0, body.Length);
                    return SlowEquals(signature, hash);
                }
            }
            catch
            {
                return false;
            }
        }

        private static bool SlowEquals(byte[] a, byte[] b)
        {
            uint diff = (uint)a.Length ^ (uint)b.Length;
            for (int i = 0; i < a.Length && i < b.Length; i++)
                diff |= (uint)(a[i] ^ b[i]);
            return diff == 0;
        }

        public async Task<LineUser> GetLineUser(string accessToken)
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var response = await httpClient.GetAsync("https://api.line.me/v2/profile");
            return JsonHelper.Deserialize<LineUser>(await response.Content.ReadAsStringAsync());
        }

        public async Task<FriendshipStatusResponse> GetFriendshipStatus(string accessToken)
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var response = await httpClient.GetAsync("https://api.line.me/friendship/v1/status");
            return JsonHelper.Deserialize<FriendshipStatusResponse>(await response.Content.ReadAsStringAsync());
        }
    }

    public class LineModels
    {
        public class LineUser
        {
            [JsonPropertyName("userId")]
            public string UserId { get; set; }
            [JsonPropertyName("displayName")]
            public string DisplayName { get; set; }
            [JsonPropertyName("pictureUrl")]
            public string PictureUrl { get; set; }
            [JsonPropertyName("statusMessage")]
            public string StatusMessage { get; set; }
        }

        public class TokenResponse
        {
            [JsonPropertyName("access_token")]
            public string AccessToken { get; set; }
            [JsonPropertyName("id_token")]
            public string IdToken { get; set; }
            [JsonIgnore]
            public JWTPayload JWTPayload { get; set; }
        }

        public class FriendshipStatusResponse
        {
            /// <summary>
            /// true if the user has added the bot as a friend and has not blocked the bot. Otherwise, false.
            /// </summary>
            [JsonPropertyName("friendFlag")]
            public bool FriendFlag { get; set; }
        }

        public class JWTPayload
        {
            /// <summary>
            /// https://access.line.me. URL where the ID token is generated.
            /// </summary>
            [JsonPropertyName("iss")]
            public string Iss { get; set; }
            /// <summary>
            /// User ID for which the ID token is generated
            /// </summary>
            [JsonPropertyName("sub")]
            public string Sub { get; set; }
            /// <summary>
            /// Channel ID
            /// </summary>
            [JsonPropertyName("aud")]
            public string Aud { get; set; }
            /// <summary>
            /// The expiry date of the token. UNIX time.
            /// </summary>
            [JsonPropertyName("exp")]
            public int Exp { get; set; }
            /// <summary>
            /// Time that the ID token was generated. UNIX time.
            /// </summary>
            [JsonPropertyName("iat")]
            public int Iat { get; set; }
            /// <summary>
            /// The nonce value specified in the authorization URL. Not included if the nonce value was not specified in the authorization request.
            /// </summary>
            [JsonPropertyName("nonce")]
            public string Nonce { get; set; }
            /// <summary>
            /// User's display name. Not included if the profile scope was not specified in the authorization request.
            /// </summary>
            [JsonPropertyName("name")]
            public string Name { get; set; }
            /// <summary>
            /// User's profile image URL. Not included if the profile scope was not specified in the authorization request.
            /// </summary>
            [JsonPropertyName("picture")]
            public string Picture { get; set; }
            /// <summary>
            /// User's email address. Not included if the email scope was not specified in the authorization request.
            /// </summary>
            [JsonPropertyName("email")]
            public string Email { get; set; }
        }
    }
}

