using IFramework.DependencyInjection;
using IFramework.Repositories;
using IFramework.UnitOfWork;

namespace IFramework.Test.EntityFramework
{
    public class DemoRepository : DomainRepository, IDemoRepository
    {
        public DemoRepository(IObjectProvider objectProvider)
            : base(objectProvider) { }
    }
}