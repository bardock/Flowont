namespace Bardock.Flowont.Tests.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class TicketPathLog : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.TicketPathLog",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        TicketID = c.Int(nullable: false),
                        ActionID = c.Int(nullable: false),
                        NodeToID = c.Int(nullable: false),
                        CreatedOn = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.Ticket", t => t.TicketID, cascadeDelete: true)
                .Index(t => t.TicketID);
        }

        public override void Down()
        {
            DropForeignKey("dbo.TicketPathLog", "TicketID", "dbo.Ticket");
            DropIndex("dbo.TicketPathLog", new[] { "TicketID" });
            DropTable("dbo.TicketPathLog");
        }
    }
}