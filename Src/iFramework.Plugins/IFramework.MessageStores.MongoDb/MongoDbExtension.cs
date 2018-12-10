using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Blueshift.EntityFrameworkCore.MongoDB.Storage;
using IFramework.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace IFramework.MessageStores.MongoDb
{
    public static class MongoDbExtension
    {
        public static MongoDbConnection GetMongoDbConnection(this DbContext dbContext)
        {
            var creator = dbContext.Database.GetPropertyValue<MongoDbDatabaseCreator>("DatabaseCreator");
            var connection = creator.GetType()
                                    ?.GetField("_mongoDbConnection", BindingFlags.NonPublic | BindingFlags.Instance)
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

        public static IMongoCollection<TEntity> GetCollection<TEntity>(this DbContext dbContext, string collectionName = null)
        {
            return string.IsNullOrWhiteSpace(collectionName) ? dbContext.GetMongoDbConnection().GetCollection<TEntity>() :
                dbContext.GetMongoDbDatabase().GetCollection<TEntity>(collectionName);
        }

        public static async Task<List<TEntity>> ToListAsync<TEntity>(this IQueryable<TEntity> queryable)
            where TEntity : class
        {
            var cursor = await ((IMongoQueryable<TEntity>)queryable).ToCursorAsync();
            return await cursor.ToListAsync();
        }

        public static async Task<TEntity[]> ToArrayAsync<TEntity>(this IQueryable<TEntity> queryable)
            where TEntity : class
        {
            return (await queryable.ToListAsync()).ToArray();
        }
    }
}
