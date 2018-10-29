using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using IFramework.Exceptions;
using IFramework.Infrastructure;
using IFramework.Infrastructure.Mailboxes;
using IFramework.UnitOfWork;
using Sample.Command.Community;
using Sample.Domain;
using Sample.Domain.Model;

namespace Sample.Applications
{
    public class CommunityService : ICommunityService
    {
        public static ConcurrentDictionary<string, int> MailboxValues = new ConcurrentDictionary<string, int>();
        private readonly IConcurrencyProcessor _concurrencyProcessor;
        private readonly IMailboxProcessor _mailboxProcessor;
        private readonly ICommunityRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public CommunityService(ICommunityRepository repository,
                                IUnitOfWork unitOfWork,
                                IConcurrencyProcessor concurrencyProcessor,
                                IMailboxProcessor mailboxProcessor)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _concurrencyProcessor = concurrencyProcessor;
            _mailboxProcessor = mailboxProcessor;
        }

        public async Task<(string, int)> MailboxTestAsync(MailboxRequest request)
        {
            (string, int) result = (null, 0);
            await _mailboxProcessor.Process(request.Id, async () =>
            {
                await Task.Delay(20).ConfigureAwait(false);

                if (MailboxValues.TryGetValue(request.Id, out var value))
                {
                    value = value + request.Number;
                    MailboxValues[request.Id] = value;
                }
                else
                {
                    value = request.Number;
                    MailboxValues.TryAdd(request.Id, value);
                }

                //throw new Exception("Test Exception");
                result = (request.Id, value);
            });
            return result;
        }

        public object GetMailboxValues()
        {
            return MailboxValues;
        }

        public async Task ModifyUserEmailAsync(Guid userId, string email)
        {
            var account = await _repository.FindAsync<Account>(a => a.Id == userId);
            if (account == null)
            {
                account = await _repository.FindAsync<Account>(a => a.UserName == "ivan");
                if (account == null)
                {
                    throw new DomainException(1, "UserNotExists");
                }
            }

            account.Modify(email);
            await _unitOfWork.CommitAsync();
        }
    }
}