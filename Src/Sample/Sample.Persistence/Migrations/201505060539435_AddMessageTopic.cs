namespace Sample.Persistence.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddMessageTopic : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.UnPublishedEvents", "Topic", c => c.String());
            AddColumn("dbo.UnSentCommands", "Topic", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.UnSentCommands", "Topic");
            DropColumn("dbo.UnPublishedEvents", "Topic");
        }
    }
}
