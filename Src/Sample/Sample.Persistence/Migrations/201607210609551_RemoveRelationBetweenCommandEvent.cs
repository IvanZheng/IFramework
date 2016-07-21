namespace Sample.Persistence.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RemoveRelationBetweenCommandEvent : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Commands", "CorrelationID", c => c.String());
            AlterColumn("dbo.Events", "CorrelationID", c => c.String());
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Events", "CorrelationID", c => c.String(maxLength: 128));
            AlterColumn("dbo.Commands", "CorrelationID", c => c.String(maxLength: 128));
        }
    }
}
