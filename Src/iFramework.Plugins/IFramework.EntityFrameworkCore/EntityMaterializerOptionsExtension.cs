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
        private readonly ExtensionEntityMaterializerSource _extensionEntityMaterializerSource = new ExtensionEntityMaterializerSource();
        internal void SetDbContext(MsDbContext dbContext)
        {
            _extensionEntityMaterializerSource.SetDbContext(dbContext);

        }

        public bool ApplyServices(IServiceCollection services)
        {
            services.AddScoped<IEntityMaterializerSource>(provider => _extensionEntityMaterializerSource);
            return false;
        }

        public long GetServiceProviderHashCode()
        {
            return _extensionEntityMaterializerSource.GetHashCode();
        }

        public void Validate(IDbContextOptions options)
        {
        }

        public string LogFragment { get; }
    }
}
