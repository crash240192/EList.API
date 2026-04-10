using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EList.Api.Extensions
{
    /// <summary>
    /// Класс методов расширения контроллера
    /// </summary>
    public static class TokenExtension
    {
        /// <summary>
        /// Получение токена авторизации
        /// </summary>
        /// <param name="controller">Контроллер для метода расширения</param>
        /// <returns>Токен авторизации</returns>
        public static Guid GetToken(this ControllerBase controller)
        {
            var claim = controller.User.Claims.First(i => i.Type == ClaimTypes.PrimarySid);
            return Guid.Parse(claim.Value);
        }
    }
}
