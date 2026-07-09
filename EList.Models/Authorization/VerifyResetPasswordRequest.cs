namespace EList.Models.Authorization
{
    public class VerifyResetPasswordRequest
    {
        public string Login { get; set; }
        public string Code { get; set; }
    }
}
