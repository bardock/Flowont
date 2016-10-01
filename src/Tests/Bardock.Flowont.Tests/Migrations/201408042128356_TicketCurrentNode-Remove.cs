namespace Bardock.Flowont.Tests.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class TicketCurrentNodeRemove : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.TicketCurrentNode", "TicketID", "dbo.Ticket");
            DropIndex("dbo.TicketCurrentNode", new[] { "TicketID" });
            AddColumn("dbo.Ticket", "CurrentNodeID", c => c.Int(nullable: false));
            DropTable("dbo.TicketCurrentNode");
        }

        public override void Down()
        {
            CreateTable(
                "dbo.TicketCurrentNode",
                c => new
                    {
                        TicketID = c.Int(nullable: false),
                        NodeID = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.TicketID, t.NodeID });

            DropColumn("dbo.Ticket", "CurrentNodeID");
            CreateIndex("dbo.TicketCurrentNode", "TicketID");
            AddForeignKey("dbo.TicketCurrentNode", "TicketID", "dbo.Ticket", "ID", cascadeDelete: true);
        }
    }
}