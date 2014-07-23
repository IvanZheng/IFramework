using IFramework.EntityFramework.Repositories;
using IFramework.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sample.Persistence.Repositories
{
    public class CommunityRepository : DomainRepository
    {
        public CommunityRepository(SampleModelContext context, IUnitOfWork unitOfWork)
            : base(context, unitOfWork)
        {

        }
    }
}
