using EList.DbDataProvider.DataConnections;
using EList.DbDataProvider.Interfaces;
using LinqToDB.Data;

namespace EList.DbDataProvider.DataProviders
{
    public class DataConnectionProvider : IDataConnectionProvider, IDisposable
    {
        private ElistDataConnection _connection;
        private DataConnectionTransaction _transaction;

        public DataConnectionProvider()
        {
            //_connection = GetConnection();
            //_connection.BeginTransaction();
        }

        public void Configure(string connectionStringName)
        {
            NpgsqlEnumMappings.Register();
            ElistDataConnection.Configure(new[] { connectionStringName });
        }

        public void Configure()
        {
            NpgsqlEnumMappings.Register();
            ElistDataConnection.Configure();
        }

        public ElistDataConnection GetConnection()
        {
            if (_connection == null)
            {
                NpgsqlEnumMappings.Register();
                _connection = new ElistDataConnection();
                NpgsqlEnumMappings.ReloadConnectionTypes(_connection);
            }
            return _connection;
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction == null)
                await StartNewTransactionAsync();
            await _transaction.CommitAsync();
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
                await _transaction.RollbackAsync();
        }

        public async Task StartNewTransactionAsync()
        {
            var connection = GetConnection();
            if (_connection.Transaction == null)
                _transaction = await connection.BeginTransactionAsync();
        }

        public void Dispose()
        {
            if (_transaction != null)
                _transaction.Commit();
            if (_connection != null)
                _connection.Close();
            GC.SuppressFinalize(this);
        }
    }
}
