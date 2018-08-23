using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using IFramework.DependencyInjection;
using IFramework.Exceptions;
using IFramework.Infrastructure;
using IFramework.UnitOfWork;
using Sample.Domain;
using Sample.Domain.Model;

namespace Sample.Applications
{
    public class CommunityService : ICommunityService
    {
        private readonly ICommunityRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConcurrencyProcessor _concurrencyProcessor;

        public CommunityService(ICommunityRepository repository, IUnitOfWork unitOfWork, IConcurrencyProcessor concurrencyProcessor)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _concurrencyProcessor = concurrencyProcessor;
        }

        [ConcurrentProcess]
        [Transaction]
        public async Task ModifyUserEmailAsync(Guid userId, string email)
        {
            var account = await _repository.FindAsync<Account>(a => a.UserName == "ivan");
            if (account == null)
            {
                throw new DomainException(1, "UserNotExists");
            }
            account.Modify(email);
            await _unitOfWork.CommitAsync();
        }
    }
}
