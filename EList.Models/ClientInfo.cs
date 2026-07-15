using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EList.Models
{
    public class ClientInfo
    {
        // Сетевые данные
        public string IP { get; set; }
        public string ForwardedIP { get; set; }
        public string RealIP { get; set; }
        public int? Port { get; set; }
        public string Protocol { get; set; }

        // Устройство
        public string UserAgent { get; set; }
        public string Platform { get; set; } // Ваш X-Client-Platform
        public string OS { get; set; }
        public string Browser { get; set; }
        public string DeviceType { get; set; } // mobile, desktop, tablet

        // Приложение
        public string AppVersion { get; set; } // Ваш X-App-Version
        public string AppBuild { get; set; } // X-App-Build

        // Запрос
        public string Method { get; set; }
        public string Url { get; set; }
        public string Path { get; set; }
        public string QueryString { get; set; }
        public string Referer { get; set; }
        public string AcceptLanguage { get; set; }
        public string Accept { get; set; }
        public string AcceptEncoding { get; set; }

        // Идентификаторы
        public string ClientId { get; set; }
        public string CorrelationId { get; set; }
        public string SessionId { get; set; }

        // Геолокация (опционально)
        public string Country { get; set; }
        public string Region { get; set; }
        public string City { get; set; }
        public string Timezone { get; set; }
        public string ISP { get; set; }

        // Временные метки
        public DateTime RequestTime { get; set; } = DateTime.UtcNow;
        public DateTime? ClientTime { get; set; } // X-Client-Time
    }
}
