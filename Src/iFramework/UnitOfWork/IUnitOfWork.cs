using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFramework.Bus;
using IFramework.Repositories;
using IFramework.Domain;
using IFramework.Message;
using System.Threading.Tasks;

namespace IFramework.UnitOfWork
{
    public interface IUnitOfWork: IDisposable
    {
        void Commit();
        Task CommitAsync();
        void Rollback();
    }
}
