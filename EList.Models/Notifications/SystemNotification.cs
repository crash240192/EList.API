using EList.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EList.Models.Notifications
{
    /// <summary>
    /// Уведомление
    /// </summary>
    public class SystemNotification
    {
        /// <summary>
        /// Идентификатор записи
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Тип уведомления
        /// </summary>
        public SystemNotificationType Type { get; set; }

        /// <summary>
        /// Заголовок уведомления
        /// </summary>
        public string Header { get; set; }

        /// <summary>
        /// Сообщение
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Краткий текст сообщения
        /// </summary>
        public string ShortMessage { get; set; }
    }
}
