 using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFramework.Bus;
using IFramework.Repositories;
using IFramework.Domain;
using IFramework.Message;

namespace IFramework.UnitOfWork
{
    public interface IUnitOfWork: IDisposable
    {
        void Commit();
        void Rollback();
    }
}
