using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace EList.Services.Impl
{
    /// <summary>
    /// Менеджер WebSocket-соединений.
    /// Хранит активные соединения, привязанные к accountId.
    /// Регистрируется как Singleton в DI-контейнере.
    /// </summary>
    public class WebSocketConnectionManager
    {
        private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<string, WebSocket>> _connections = new();

        /// <summary>
        /// Добавить соединение для аккаунта.
        /// У одного аккаунта может быть несколько соединений (несколько вкладок / устройств).
        /// </summary>
        public string AddConnection(Guid accountId, WebSocket socket)
        {
            var connectionId = Guid.NewGuid().ToString("N");
            var accountSockets = _connections.GetOrAdd(accountId, _ => new ConcurrentDictionary<string, WebSocket>());
            accountSockets[connectionId] = socket;
            return connectionId;
        }

        /// <summary>
        /// Удалить конкретное соединение
        /// </summary>
        public void RemoveConnection(Guid accountId, string connectionId)
        {
            if (_connections.TryGetValue(accountId, out var accountSockets))
            {
                accountSockets.TryRemove(connectionId, out _);

                if (accountSockets.IsEmpty)
                    _connections.TryRemove(accountId, out _);
            }
        }

        /// <summary>
        /// Получить все активные сокеты конкретного аккаунта
        /// </summary>
        public IEnumerable<WebSocket> GetConnections(Guid accountId)
        {
            if (_connections.TryGetValue(accountId, out var accountSockets))
                return accountSockets.Values.Where(s => s.State == WebSocketState.Open);

            return Enumerable.Empty<WebSocket>();
        }

        /// <summary>
        /// Получить все активные сокеты перечисленных аккаунтов
        /// </summary>
        public IEnumerable<WebSocket> GetConnections(List<Guid> accountIds)
        {
            var sockets = _connections.Where(i => accountIds.Contains(i.Key))
                ?.SelectMany(i => i.Value.Values)
                ?.Where(i => i.State == WebSocketState.Open);
           
            return sockets ?? Enumerable.Empty<WebSocket>();
        }

        /// <summary>
        /// Получить все активные сокеты всех аккаунтов (для broadcast)
        /// </summary>
        public IEnumerable<WebSocket> GetAllConnections()
        {
            return _connections.Values
                .SelectMany(dict => dict.Values)
                .Where(s => s.State == WebSocketState.Open);
        }

        /// <summary>
        /// Количество подключённых аккаунтов
        /// </summary>
        public int ConnectedAccountsCount => _connections.Count;

        /// <summary>
        /// Общее количество активных соединений
        /// </summary>
        public int TotalConnectionsCount => _connections.Values.Sum(d => d.Count);

        /// <summary>
        /// Есть ли хотя бы одно открытое WebSocket-соединение у аккаунта
        /// </summary>
        public bool IsOnline(Guid accountId)
        {
            return GetConnections(accountId).Any();
        }

        /// <summary>
        /// Из переданных id вернуть тех, кто сейчас онлайн (есть открытый WS)
        /// </summary>
        public List<Guid> FilterOnline(IEnumerable<Guid> accountIds)
        {
            if (accountIds == null)
                return new List<Guid>();

            var result = new List<Guid>();
            foreach (var id in accountIds.Distinct())
            {
                if (IsOnline(id))
                    result.Add(id);
            }
            return result;
        }
    }
}
