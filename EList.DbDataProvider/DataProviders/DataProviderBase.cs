using EList.DbDataProvider.DataConnections;
using EList.DbDataProvider.Interfaces;

namespace EList.DbDataProvider.DataProviders
{
    public abstract class DataProviderBase
    {
        private readonly IDataConnectionProvider _connectionProvider;
        protected ElistDataConnection _connection
        {
            get
            {
                return _connectionProvider.GetConnection();
            }
        }

        public DataProviderBase(IDataConnectionProvider dataConnectionProvider)
        {
            _connectionProvider = dataConnectionProvider;
        }
    }
}
