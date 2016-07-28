using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFramework.UnitOfWork
{
    public class MockUnitOfWork : IUnitOfWork
    {
        public void Dispose()
        {

        }

        public void Commit()
        {
           
        }

        public void Rollback()
        {
        }

        public Task CommitAsync()
        {
            return null;
        }
    }
}
