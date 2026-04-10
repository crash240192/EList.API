using EList.Common.Support;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using System.Security.Claims;

namespace EList.Api.Extensions
{
    public static  class ClientInfoExtension
    {
        public static string GetClientHash(this ControllerBase controller)
        {
            var claim = controller.User.Claims.FirstOrDefault(i => i.Type == ClaimTypes.Hash);

            if (claim == null)
                throw new ArgumentNullException("Authorization-jwt", "Не указан заголовок Authorization-jwt");

            return claim.Value;

            //var ipAddress = controller.Request.HttpContext.Connection.RemoteIpAddress?.ToString();
            //var userAgent = controller.Request.Headers.UserAgent;
            //var userAgentHash = EncryptionUtility.EncryptMD5($"{ipAddress} - {userAgent}");

            //return userAgentHash;
        }
    }
}
