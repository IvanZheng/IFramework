using IFramework.EntityFramework.Repositories;
using IFramework.IoC;
using IFramework.UnitOfWork;
using Sample.Domain;

namespace Sample.Persistence.Repositories
{
    public class CommunityRepository : DomainRepository, ICommunityRepository
    {
        public CommunityRepository(SampleModelContext context, IUnitOfWork unitOfWork, IContainer container)
            : base(context, unitOfWork, container)
        {
        }
    }
}