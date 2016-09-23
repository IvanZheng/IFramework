namespace Sample.Persistence.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddSagaInfo : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.UnPublishedEvents", "ReplyToEndPoint", c => c.String());
            AddColumn("dbo.UnPublishedEvents", "SagaInfo_SagaId", c => c.String());
            AddColumn("dbo.UnPublishedEvents", "SagaInfo_ReplyEndPoint", c => c.String());
            AddColumn("dbo.UnSentCommands", "ReplyToEndPoint", c => c.String());
            AddColumn("dbo.UnSentCommands", "SagaInfo_SagaId", c => c.String());
            AddColumn("dbo.UnSentCommands", "SagaInfo_ReplyEndPoint", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.UnSentCommands", "SagaInfo_ReplyEndPoint");
            DropColumn("dbo.UnSentCommands", "SagaInfo_SagaId");
            DropColumn("dbo.UnSentCommands", "ReplyToEndPoint");
            DropColumn("dbo.UnPublishedEvents", "SagaInfo_ReplyEndPoint");
            DropColumn("dbo.UnPublishedEvents", "SagaInfo_SagaId");
            DropColumn("dbo.UnPublishedEvents", "ReplyToEndPoint");
        }
    }
}
