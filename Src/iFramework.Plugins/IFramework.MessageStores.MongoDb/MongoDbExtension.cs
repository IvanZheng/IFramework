using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Blueshift.EntityFrameworkCore.MongoDB.Storage;
using IFramework.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MongoDB.Driver;

namespace IFramework.MessageStores.MongoDb
{
    public static class MongoDbExtension
    {
        public static MongoDbConnection GetMongoDbConnection(this DbContext dbContext)
        {
            var creator = dbContext.Database.GetPropertyValue<MongoDbDatabaseCreator>("DatabaseCreator");
            var connection = creator.GetType()
                                    ?.GetField("_mongoDbConnection", BindingFlags.NonPublic  | BindingFlags.Instance)
                                    ?.GetValue(creator) as MongoDbConnection;
            return connection;
        }

        public static MongoClient GetMongoDbClient(this DbContext dbContext)
        {
            var connection = dbContext.GetMongoDbConnection();
            return connection?.GetType()
                             .GetField("_mongoClient", BindingFlags.NonPublic | BindingFlags.Instance)
                             ?.GetValue(connection) as MongoClient;
        }

        public static IMongoDatabase GetMongoDbDatabase(this DbContext dbContext)
        {
            var connection = dbContext.GetMongoDbConnection();

            return connection?.GetType()
                             .GetField("_mongoDatabase", BindingFlags.NonPublic | BindingFlags.Instance)
                             ?.GetValue(connection) as IMongoDatabase;
        }
    }
}
