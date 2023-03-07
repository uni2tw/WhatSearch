using Microsoft.AspNetCore.Http;

namespace WhatSearch.DataAccess
{
    public interface IHttpContextService
    {
        /// <summary>
        /// 取得目前登入者Id
        /// </summary>
        /// <returns></returns>
        int GetCurrentUserId();
    }

    public class HttpContextService : IHttpContextService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        public HttpContextService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }


        public int GetCurrentUserId()
        {
            int personalId;
            var userIdentity = _httpContextAccessor.HttpContext?.User.Identity;
            if (userIdentity != null && userIdentity.IsAuthenticated &&
                    int.TryParse(userIdentity.Name, out personalId))
            {
                return personalId;
            }
            return 0;
        }
    }
}
