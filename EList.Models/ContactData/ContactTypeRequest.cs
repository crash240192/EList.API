using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EList.Models.ContactData
{
    /// <summary>
    /// Класс-контейнер запроса на создание типа контактных данных
    /// </summary>
    public class ContactTypeRequest
    {
        /// <summary>
        /// Путь до значение в файле локализации
        /// </summary>
        public string NamePath { get; set; }
        
        /// <summary>
        /// Описание типа контакта
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Маска
        /// </summary>
        public string Mask { get; set; }

        /// <summary>
        /// Флаг доступности типа контакта для рассылки уведомлений
        /// </summary>
        public bool AllowNotifications { get; set; }
    }
}
