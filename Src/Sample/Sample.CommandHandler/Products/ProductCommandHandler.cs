using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IFramework.Command;
using IFramework.Message;
using IFramework.UnitOfWork;
using Sample.Command;
using Sample.Domain;
using Sample.Domain.Model;
using Sample.DTO;

namespace Sample.CommandHandler.Products
{
    public class ProductCommandHandler : ICommandHandler<CreateProduct>,
                                        ICommandAsyncHandler<ReduceProduct>,
                                        ICommandHandler<GetProducts>
    {
        //   IEventBus _EventBus;
        private readonly IMessageContext _commandContext;

        private readonly ICommunityRepository _domainRepository;
        private readonly IUnitOfWork _unitOfWork;

        public ProductCommandHandler(IUnitOfWork unitOfWork,
                                    ICommunityRepository domainRepository,
                                    // IEventBus eventBus,
                                    IMessageContext commandContext)
        {
            _unitOfWork = unitOfWork;
            _domainRepository = domainRepository;
            _commandContext = commandContext;
            // _EventBus = eventBus;
        }

        public async Task Handle(ReduceProduct command, CancellationToken cancellationToken)
        {
            var product = await _domainRepository.GetByKeyAsync<Product>(command.ProductId)
                                                 .ConfigureAwait(false);
            product.ReduceCount(command.ReduceCount);
            await _unitOfWork.CommitAsync()
                             .ConfigureAwait(false);
            _commandContext.Reply = product.Count;
        }

        public void Handle(CreateProduct command)
        {
            var product = new Product(command.ProductId, command.Name, command.Count);
            _domainRepository.Add(product);
            _unitOfWork.Commit();
        }

        public void Handle(GetProducts command)
        {
            var products = _domainRepository.FindAll<Product>(p => command.ProductIds.Contains(p.Id))
                                            .Select(p => new Project {Id = p.Id, Name = p.Name, Count = p.Count})
                                            .ToList();
            _commandContext.Reply = products;
        }
    }
}