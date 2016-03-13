namespace Sample.Persistence.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddProjectVersion : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Products", "Version", c => c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Products", "Version");
        }
    }
}
