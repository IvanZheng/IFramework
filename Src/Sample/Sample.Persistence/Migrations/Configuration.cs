namespace Sample.Persistence.Migrations
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;
    using IFramework.Config;
    using System.Configuration;
    using Microsoft.Practices.Unity.Configuration;
    using Microsoft.Practices.Unity;

    internal sealed class Configuration : DbMigrationsConfiguration<Sample.Persistence.SampleModelContext>
    {
        public Configuration()
        {
            IFramework.Config.Configuration.Instance.UseLog4Net();

            AutomaticMigrationsEnabled = true;
        }

        protected override void Seed(Sample.Persistence.SampleModelContext context)
        {
            //  This method will be called after migrating to the latest version.

            //  You can use the DbSet<T>.AddOrUpdate() helper extension method 
            //  to avoid creating duplicate seed data. E.g.
            //
            //    context.People.AddOrUpdate(
            //      p => p.FullName,
            //      new Person { FullName = "Andrew Peters" },
            //      new Person { FullName = "Brice Lambson" },
            //      new Person { FullName = "Rowan Miller" }
            //    );
            //
        }
    }
}
