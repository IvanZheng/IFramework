namespace Sample.Persistence.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddProductCreateTime : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Products", "CreateTime", c => c.DateTime(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Products", "CreateTime");
        }
    }
}
