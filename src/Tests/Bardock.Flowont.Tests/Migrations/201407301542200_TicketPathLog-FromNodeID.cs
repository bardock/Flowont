namespace Bardock.Flowont.Tests.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class TicketPathLogFromNodeID : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.TicketPathLog", "FromNodeID", c => c.Int(nullable: false));
            AddColumn("dbo.TicketPathLog", "ToNodeID", c => c.Int(nullable: false));
            DropColumn("dbo.TicketPathLog", "NodeToID");
        }

        public override void Down()
        {
            AddColumn("dbo.TicketPathLog", "NodeToID", c => c.Int(nullable: false));
            DropColumn("dbo.TicketPathLog", "ToNodeID");
            DropColumn("dbo.TicketPathLog", "FromNodeID");
        }
    }
}