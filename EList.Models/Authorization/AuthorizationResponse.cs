using EList.Models.Enums;

namespace EList.Models.Authorization
{
    public class AuthorizationResponse
    {
        /// <summary>
        /// Токен доступа
        /// </summary>
        public Guid Token { get; set; }

        /// <summary>
        /// Требуется ли активация токена
        /// </summary>
        public bool ActivationRequired { get; set; }
    }
}
