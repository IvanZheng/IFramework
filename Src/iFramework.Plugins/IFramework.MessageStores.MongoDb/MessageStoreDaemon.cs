using System.Linq;
using IFramework.MessageStores.Abstracts;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace IFramework.MessageStores.MongoDb
{
    public class MessageStoreDaemon : Abstracts.MessageStoreDaemon
    {
        protected override void RemoveUnSentCommands(Abstracts.MessageStore messageStore, string[] toRemoveCommands)
        {
            if (messageStore.InMemoryStore)
            {
                base.RemoveUnSentCommands(messageStore, toRemoveCommands);
                return;
            }
            messageStore.GetMongoDbDatabase()
                        .GetCollection<UnSentCommand>("unSentCommands")
                        .DeleteMany(Builders<UnSentCommand>.Filter
                                                           .AnyIn("_id", toRemoveCommands));
        }

        protected override void RemoveUnPublishedEvents(Abstracts.MessageStore messageStore, string[] toRemoveEvents)
        {
            if (messageStore.InMemoryStore)
            {
                base.RemoveUnPublishedEvents(messageStore, toRemoveEvents);
                return;
            }

            messageStore.GetMongoDbDatabase()
                        .GetCollection<UnPublishedEvent>("unPublishedEvents")
                        .DeleteMany(Builders<UnPublishedEvent>.Filter
                                                              .AnyIn("_id", toRemoveEvents));
        }

        public MessageStoreDaemon(ILogger<Abstracts.MessageStoreDaemon> logger) : base(logger) { }
    }
}