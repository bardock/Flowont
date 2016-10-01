namespace Bardock.Flowont.Tests.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class TicketCurrentNodeRename : DbMigration
    {
        public override void Up()
        {
            RenameTable(name: "dbo.TicketNode", newName: "TicketCurrentNode");
            DropPrimaryKey("dbo.TicketCurrentNode");
            AddColumn("dbo.TicketCurrentNode", "NodeID", c => c.Int(nullable: false));
            AddPrimaryKey("dbo.TicketCurrentNode", new[] { "TicketID", "NodeID" });
            DropColumn("dbo.TicketCurrentNode", "ID");
        }

        public override void Down()
        {
            AddColumn("dbo.TicketCurrentNode", "ID", c => c.Int(nullable: false));
            DropPrimaryKey("dbo.TicketCurrentNode");
            DropColumn("dbo.TicketCurrentNode", "NodeID");
            AddPrimaryKey("dbo.TicketCurrentNode", new[] { "TicketID", "ID" });
            RenameTable(name: "dbo.TicketCurrentNode", newName: "TicketNode");
        }
    }
}