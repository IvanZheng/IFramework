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
        ICommandHandler<ReduceProduct>,
        ICommandHandler<GetProduct>
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

        public void Handle(GetProduct command)
        {
            var product = _DomainRepository.GetByKey<Product>(command.ProductId);
            _CommandContext.Reply = product.Count;
        }

        public void Handle(ReduceProduct command)
        {
            var product = _DomainRepository.GetByKey<Product>(command.ProductId);
            product.ReduceCount(command.ReduceCount);
            _UnitOfWork.Commit();
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
