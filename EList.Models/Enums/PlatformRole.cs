namespace EList.Models.Enums
{
    /// <summary>
    /// Роль площадки. Отсутствие роли = обычный пользователь.
    /// </summary>
    public enum PlatformRole
    {
        Superuser = 0,
        Admin = 1,
        Moderator = 2
    }
}
