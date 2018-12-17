using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Blueshift.EntityFrameworkCore.MongoDB.Storage;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using IFramework.Infrastructure;

namespace Blueshift.EntityFrameworkCore.MongoDB
{
    /// <summary>
    /// 
    /// </summary>
    public static class MongoDbExtension
    {
        /// <summary>
        /// GetMongoDbConnection
        /// </summary>
        /// <param name="dbContext"></param>
        /// <returns></returns>
        public static MongoDbConnection GetMongoDbConnection(this DbContext dbContext)
        {
            var creator = dbContext.Database.GetPropertyValue<MongoDbDatabaseCreator>("DatabaseCreator");
            var connection = creator.GetType()
                                    ?.GetField("_mongoDbConnection", BindingFlags.NonPublic | BindingFlags.Instance)
                                    ?.GetValue(creator) as MongoDbConnection;
            return connection;
        }

        /// <summary>
        /// GetMongoDbClient
        /// </summary>
        /// <param name="dbContext"></param>
        /// <returns></returns>
        public static MongoClient GetMongoDbClient(this DbContext dbContext)
        {
            var connection = dbContext.GetMongoDbConnection();
            return connection?.GetType()
                             .GetField("_mongoClient", BindingFlags.NonPublic | BindingFlags.Instance)
                             ?.GetValue(connection) as MongoClient;
        }

        /// <summary>
        /// GetMongoDbDatabase
        /// </summary>
        /// <param name="dbContext"></param>
        /// <returns></returns>
        public static IMongoDatabase GetMongoDbDatabase(this DbContext dbContext)
        {
            var connection = dbContext.GetMongoDbConnection();

            return connection?.GetType()
                             .GetField("_mongoDatabase", BindingFlags.NonPublic | BindingFlags.Instance)
                             ?.GetValue(connection) as IMongoDatabase;
        }

        /// <summary>
        /// GetCollection
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="dbContext"></param>
        /// <param name="collectionName"></param>
        /// <returns></returns>
        public static IMongoCollection<TEntity> GetCollection<TEntity>(this DbContext dbContext, string collectionName = null)
        {
            return string.IsNullOrWhiteSpace(collectionName) ? dbContext.GetMongoDbConnection().GetCollection<TEntity>() :
                dbContext.GetMongoDbDatabase().GetCollection<TEntity>(collectionName);
        }


        /// <summary>
        /// ToListAsync
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="queryable"></param>
        /// <returns></returns>
        public static async Task<List<TEntity>> ToListAsync<TEntity>(this IQueryable<TEntity> queryable)
            where TEntity : class
        {
            if (queryable is IMongoQueryable<TEntity>)
            {
                var cursor = await ((IMongoQueryable<TEntity>)queryable).ToCursorAsync()
                                                                        .ConfigureAwait(false);
                return await cursor.ToListAsync()
                                   .ConfigureAwait(false);
            }
            else
            {
                return await EntityFrameworkQueryableExtensions.ToListAsync(queryable)
                                                               .ConfigureAwait(false);
            }
        }

        /// <summary>
        /// ToArrayAsync
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="queryable"></param>
        /// <returns></returns>
        public static async Task<TEntity[]> ToArrayAsync<TEntity>(this IQueryable<TEntity> queryable)
            where TEntity : class
        {
            if (queryable is IMongoQueryable<TEntity>)
            {
                return (await queryable.ToListAsync()
                                       .ConfigureAwait(false)).ToArray();
            }
            else
            {
                return await EntityFrameworkQueryableExtensions.ToArrayAsync(queryable)
                                                               .ConfigureAwait(false);
            }
        }

        /// <summary>
        /// CountAsync
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="queryable"></param>
        /// <returns></returns>
        public static Task<int> CountAsync<TEntity>(this IQueryable<TEntity> queryable)
            where TEntity : class
        {
            return MongoQueryable.CountAsync((IMongoQueryable<TEntity>) queryable);
        }
    }
}
