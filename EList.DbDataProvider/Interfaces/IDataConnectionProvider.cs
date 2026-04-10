using EList.DbDataProvider.DataConnections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EList.DbDataProvider.Interfaces
{
    public interface IDataConnectionProvider : IDisposable
    {
        void Configure(string connectionStringName);
        void Configure();
        ElistDataConnection GetConnection();
        Task StartNewTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}
