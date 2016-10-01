namespace Bardock.Flowont.Tests.Migrations
{
    using System;
    using System.Data.Entity.Migrations;

    internal sealed class Configuration : DbMigrationsConfiguration<Bardock.Flowont.Tests.TicketsContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }

        protected override void Seed(Bardock.Flowont.Tests.TicketsContext context)
        {
            context.Tickets.AddOrUpdate(
                e => e.Title,
                new Ticket()
                {
                    FlowID = TicketFlows.v1,
                    CurrentNodeID = TicketNodes.Pending,
                    Title = "Pending",
                    CreatedOn = new DateTime(2014, 1, 1)
                },
                new Ticket()
                {
                    FlowID = TicketFlows.v1,
                    CurrentNodeID = TicketNodes.Approved,
                    Title = "Approved",
                    CreatedOn = new DateTime(2014, 1, 1)
                },
                new Ticket()
                {
                    FlowID = TicketFlows.v1,
                    CurrentNodeID = TicketNodes.Rejected,
                    Title = "Rejected",
                    CreatedOn = new DateTime(2014, 1, 1)
                },
                new Ticket()
                {
                    FlowID = TicketFlows.v1,
                    CurrentNodeID = TicketNodes.Closed,
                    Title = "Closed",
                    CreatedOn = new DateTime(2014, 1, 1)
                },
                new Ticket()
                {
                    FlowID = TicketFlows.v1,
                    CurrentNodeID = TicketNodes.Archived,
                    Title = "Archived",
                    CreatedOn = new DateTime(2014, 1, 1)
                }
            );
        }
    }
}