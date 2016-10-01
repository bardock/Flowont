namespace Bardock.Flowont.Tests.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class Initial : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Ticket",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        FlowID = c.Int(nullable: false),
                        Title = c.String(unicode: false),
                        CreatedOn = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.ID);

            CreateTable(
                "dbo.TicketNode",
                c => new
                    {
                        TicketID = c.Int(nullable: false),
                        ID = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.TicketID, t.ID })
                .ForeignKey("dbo.Ticket", t => t.TicketID, cascadeDelete: true)
                .Index(t => t.TicketID);
        }

        public override void Down()
        {
            DropForeignKey("dbo.TicketNode", "TicketID", "dbo.Ticket");
            DropIndex("dbo.TicketNode", new[] { "TicketID" });
            DropTable("dbo.TicketNode");
            DropTable("dbo.Ticket");
        }
    }
}