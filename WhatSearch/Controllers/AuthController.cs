using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using WhatSearch.Core;
using WhatSearch.DataProviders.Interfaces;
using WhatSearch.Models;
using WhatSearch.Services.Interfaces;
using static WhatSearch.Controllers.LineModels;

namespace WhatSearch.Controllers
{
    public class AuthController : Controller
    {
        SystemConfig config = Ioc.GetConfig();
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
            string clientId = config.Line.ClientId;
            string clientSecret = config.Line.ClientSecret;
            string redirectUrl = config.Line.Callback;

            LineLoginClient lineMgr = new LineLoginClient(clientId, clientSecret, redirectUrl);
            IUserService userService = Ioc.Get<IUserService>();
            TokenResponse tokenData = lineMgr.GetToken(code).Result;
            if (tokenData == null)
            {
                return NotFound();
            }
            LineUser lineUser = lineMgr.GetLineUser(tokenData.AccessToken).Result;
            if (lineUser == null || string.IsNullOrEmpty(lineUser.UserId))
            {
                return Content("Error to login by Line.");
            }            
            Member mem = userService.GetMember(lineUser.UserId);
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
                bool success = userService.SaveMember(mem, out accessToken);
            }
            else
            {
                accessToken = mem.AccessToken;
                userService.UpdateMember(mem.Name);
            }
            if (mem.Status == MemberStatus.Invalice)
            {
                return Content("你沒有通過認證，請在Line上跟 unicorn 說一下。");
            }
            if (string.IsNullOrEmpty(accessToken) == false)
            {
                userService.ForceLogin(Response, accessToken);
            }

            //line private
            var friendshipStatus = lineMgr.GetFriendshipStatus(tokenData.AccessToken).Result;

            if (string.IsNullOrEmpty(state) == false)
            {
                return Redirect(state);
            }
            return Content("Line: " + JsonConvert.SerializeObject(lineUser));
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
            var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(await response.Content.ReadAsStringAsync());
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
                return JsonConvert.DeserializeObject<JWTPayload>(payload);
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
            return JsonConvert.DeserializeObject<LineUser>(await response.Content.ReadAsStringAsync());
        }

        public async Task<FriendshipStatusResponse> GetFriendshipStatus(string accessToken)
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var response = await httpClient.GetAsync("https://api.line.me/friendship/v1/status");
            return JsonConvert.DeserializeObject<FriendshipStatusResponse>(await response.Content.ReadAsStringAsync());
        }
    }

    public class LineModels
    {
        public class LineUser
        {
            [JsonProperty("userId")]
            public string UserId { get; set; }
            [JsonProperty("displayName")]
            public string DisplayName { get; set; }
            [JsonProperty("pictureUrl")]
            public string PictureUrl { get; set; }
            [JsonProperty("statusMessage")]
            public string StatusMessage { get; set; }
        }

        public class TokenResponse
        {
            [JsonProperty("access_token")]
            public string AccessToken { get; set; }
            [JsonProperty("id_token")]
            public string IdToken { get; set; }
            [JsonIgnore]
            public JWTPayload JWTPayload { get; set; }
        }

        public class FriendshipStatusResponse
        {
            /// <summary>
            /// true if the user has added the bot as a friend and has not blocked the bot. Otherwise, false.
            /// </summary>
            [JsonProperty("friendFlag")]
            public bool FriendFlag { get; set; }
        }

        public class JWTPayload
        {
            /// <summary>
            /// https://access.line.me. URL where the ID token is generated.
            /// </summary>
            [JsonProperty("iss")]
            public string Iss { get; set; }
            /// <summary>
            /// User ID for which the ID token is generated
            /// </summary>
            [JsonProperty("sub")]
            public string Sub { get; set; }
            /// <summary>
            /// Channel ID
            /// </summary>
            [JsonProperty("aud")]
            public string Aud { get; set; }
            /// <summary>
            /// The expiry date of the token. UNIX time.
            /// </summary>
            [JsonProperty("exp")]
            public int Exp { get; set; }
            /// <summary>
            /// Time that the ID token was generated. UNIX time.
            /// </summary>
            [JsonProperty("iat")]
            public int Iat { get; set; }
            /// <summary>
            /// The nonce value specified in the authorization URL. Not included if the nonce value was not specified in the authorization request.
            /// </summary>
            [JsonProperty("nonce")]
            public string Nonce { get; set; }
            /// <summary>
            /// User's display name. Not included if the profile scope was not specified in the authorization request.
            /// </summary>
            [JsonProperty("name")]
            public string Name { get; set; }
            /// <summary>
            /// User's profile image URL. Not included if the profile scope was not specified in the authorization request.
            /// </summary>
            [JsonProperty("picture")]
            public string Picture { get; set; }
            /// <summary>
            /// User's email address. Not included if the email scope was not specified in the authorization request.
            /// </summary>
            [JsonProperty("email")]
            public string Email { get; set; }
        }
    }
}

