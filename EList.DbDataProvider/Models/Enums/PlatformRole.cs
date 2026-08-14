using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models.Enums
{
    /// <summary>
    /// Роль площадки. Отсутствие записи в account_platform_roles = обычный пользователь.
    /// </summary>
    public enum PlatformRole
    {
        [MapValue(Value = "superuser")]
        Superuser = 0,

        [MapValue(Value = "admin")]
        Admin = 1,

        [MapValue(Value = "moderator")]
        Moderator = 2
    }
}
