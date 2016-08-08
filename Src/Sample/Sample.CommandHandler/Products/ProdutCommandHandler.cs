using IFramework.Command;
using IFramework.Message;
using IFramework.UnitOfWork;
using Sample.Command;
using Sample.Domain.Model;
using Sample.Persistence.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.CommandHandler.Products
{
    public class ProdutCommandHandler : ICommandHandler<CreateProduct>,
        ICommandAsyncHandler<ReduceProduct>,
        ICommandHandler<GetProducts>
    {
     //   IEventBus _EventBus;
        IMessageContext _CommandContext;
        CommunityRepository _DomainRepository;
        IUnitOfWork _UnitOfWork;
        public ProdutCommandHandler(IUnitOfWork unitOfWork,
                                       CommunityRepository domainRepository,
                                      // IEventBus eventBus,
                                       IMessageContext commandContext)
        {
            _UnitOfWork = unitOfWork;
            _DomainRepository = domainRepository;
            _CommandContext = commandContext;
           // _EventBus = eventBus;
        }

        public void Handle(GetProducts command)
        {
            var products = _DomainRepository.FindAll<Product>(p => command.ProductIds.Contains(p.Id))
                                           .Select(p => new Sample.DTO.Project { Id = p.Id, Name = p.Name, Count = p.Count })
                                           .ToList();
            _CommandContext.Reply = products;
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


    }
}
