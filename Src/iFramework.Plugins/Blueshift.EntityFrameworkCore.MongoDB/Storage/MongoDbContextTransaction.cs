using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore.Storage;
using MongoDB.Driver;

namespace Blueshift.EntityFrameworkCore.MongoDB.Storage
{
    public class MongoDbContextTransaction: IDbContextTransaction 
    {
        private readonly IClientSessionHandle _session;

        public MongoDbContextTransaction(IClientSessionHandle session)
        {
            _session = session;
            TransactionId = _session.WrappedCoreSession.Id.AsGuid;
        }
        public void Dispose()
        {
            _session.Dispose();
        }

        public void Commit()
        {
            _session.CommitTransaction();
        }

        public void Rollback()
        {
         
        }

        public Guid TransactionId { get; }
    }
}
