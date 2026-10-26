namespace EList.Models.Accounts
{
    /// <summary>Установка / обновление фото шапки профиля.</summary>
    public class AccountHeaderRequest
    {
        /// <summary>Id файла в filestorage. null — сбросить шапку.</summary>
        public Guid? ImageId { get; set; }

        /// <summary>Фокус 0…100 (как coverFocusX у события). Default 50.</summary>
        public double? FocusX { get; set; }

        /// <summary>Фокус 0…100 (как coverFocusY у события). Default 50.</summary>
        public double? FocusY { get; set; }

        /// <summary>Блюр фона шапки.</summary>
        public bool Blur { get; set; }
    }
}
