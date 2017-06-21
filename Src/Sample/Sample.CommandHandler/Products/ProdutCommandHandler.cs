using System.Linq;
using System.Threading.Tasks;
using IFramework.Command;
using IFramework.Message;
using IFramework.UnitOfWork;
using Sample.Command;
using Sample.Domain;
using Sample.Domain.Model;
using Sample.DTO;
using Sample.Persistence.Repositories;

namespace Sample.CommandHandler.Products
{
    public class ProdutCommandHandler : ICommandHandler<CreateProduct>,
                                        ICommandAsyncHandler<ReduceProduct>,
                                        ICommandHandler<GetProducts>
    {
        //   IEventBus _EventBus;
        private readonly IMessageContext _CommandContext;

        private readonly ICommunityRepository _DomainRepository;
        private readonly IUnitOfWork _UnitOfWork;

        public ProdutCommandHandler(IUnitOfWork unitOfWork,
                                    ICommunityRepository domainRepository,
                                    // IEventBus eventBus,
                                    IMessageContext commandContext)
        {
            _UnitOfWork = unitOfWork;
            _DomainRepository = domainRepository;
            _CommandContext = commandContext;
            // _EventBus = eventBus;
        }

        public async Task Handle(ReduceProduct command)
        {
            var product = await _DomainRepository.GetByKeyAsync<Product>(command.ProductId)
                                                 .ConfigureAwait(false);
            product.ReduceCount(command.ReduceCount);
            await _UnitOfWork.CommitAsync()
                             .ConfigureAwait(false);
            _CommandContext.Reply = product.Count;
        }

        public void Handle(CreateProduct command)
        {
            var product = new Product(command.ProductId, command.Name, command.Count);
            _DomainRepository.Add(product);
            _UnitOfWork.Commit();
        }

        public void Handle(GetProducts command)
        {
            var products = _DomainRepository.FindAll<Product>(p => command.ProductIds.Contains(p.Id))
                                            .Select(p => new Project {Id = p.Id, Name = p.Name, Count = p.Count})
                                            .ToList();
            _CommandContext.Reply = products;
        }
    }
}