using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace IFramework.EntityFrameworkCore
{
    public class EntityMaterializerOptionsExtension: IDbContextOptionsExtension
    {
        private readonly MsDbContext _dbContext;

        public EntityMaterializerOptionsExtension(MsDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public bool ApplyServices(IServiceCollection services)
        {
            services.AddScoped<IEntityMaterializerSource>(provider => new ExtensionEntityMaterializerSource(_dbContext));
            return true;
        }

        public long GetServiceProviderHashCode()
        {
            throw new NotImplementedException();
        }

        public void Validate(IDbContextOptions options)
        {
            throw new NotImplementedException();
        }

        public string LogFragment { get; }
    }
}
