using Bardock.Utils.Extensions;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Bardock.Flowont.Tests
{
    public class FlowDefinitionsTest
    {
        private List<Ticket> GetTickets(params Ticket[] ticketsToAdd)
        {
            var tickets = new List<Ticket>()
            {
                new Ticket()
                {
                    FlowID = TicketFlows.v1,
                    CurrentNodeID = TicketNodes.Pending,
                    Title = "Pending"
                },
                new Ticket()
                {
                    FlowID = TicketFlows.v1,
                    CurrentNodeID = TicketNodes.Pending,
                    Title = null
                },
                new Ticket()
                {
                    FlowID = TicketFlows.v1,
                    CurrentNodeID = TicketNodes.Approved,
                    Title = "Approved"
                },
                new Ticket()
                {
                    FlowID = TicketFlows.v1,
                    CurrentNodeID = TicketNodes.Rejected,
                    Title = "Rejected"
                },
                new Ticket()
                {
                    FlowID = TicketFlows.v1,
                    CurrentNodeID = TicketNodes.Closed,
                    Title = "Closed"
                },
                new Ticket()
                {
                    FlowID = TicketFlows.v1,
                    CurrentNodeID = TicketNodes.Archived,
                    Title = "Archived"
                },
            };
            tickets.AddRange(ticketsToAdd);
            return tickets;
        }

        [Fact]
        public void GetIsNodeWhenExpression()
        {
            var user = new User()
            {
                RoleIDs = { Roles.OnlyApprovedAndRejected }
            };

            var expr = TicketFlowDefinitions.Instance.GetIsInNodeExpression(TicketNodes.Approved);

            var tickets = GetTickets();
            var expectedTickets = tickets.Where(x => x.CurrentNodeID == TicketNodes.Approved);

            var filteredTickets = tickets.AsQueryable().Where(expr).ToList();
            Assert.Equal(expectedTickets.Count(), filteredTickets.Count());
            Assert.True(expectedTickets.All(et => filteredTickets.Contains(et)));
        }

        [Fact]
        public void GetIsNodeExpression_EF()
        {
            var user = new User()
            {
                RoleIDs = { Roles.OnlyApprovedAndRejected }
            };

            var expr = TicketFlowDefinitions.Instance.GetIsInNodeExpression(TicketNodes.Approved);

            using (var db = new TicketsContext(TicketsContext.EffortConnection()))
            {
                var q = db.Tickets.Where(expr);
                var sql = q.ToString();
                var tickets = q.ToList();
                Assert.Equal(1, tickets.Count());
                Assert.True(tickets.All(t => t.CurrentNodeID == TicketNodes.Approved));
            }
        }

        [Fact]
        public void GetIsVisibleWhenExpression()
        {
            var user = new User()
            {
                RoleIDs = { Roles.OnlyApprovedAndRejected }
            };

            var expr = TicketFlowDefinitions.Instance.GetIsVisibleExpression(new TicketWorkFlowContext() { User = user });

            var tickets = GetTickets();
            var expectedTickets = tickets.Where(x => x.CurrentNodeID.In(TicketNodes.Approved, TicketNodes.Rejected));

            var filteredTickets = tickets.AsQueryable().Where(expr).ToList();
            Assert.Equal(expectedTickets.Count(), filteredTickets.Count());
            Assert.True(expectedTickets.All(et => filteredTickets.Contains(et)));
        }

        [Fact]
        public void GetIsVisibleWhenExpression_EF()
        {
            var user = new User()
            {
                RoleIDs = { Roles.OnlyApprovedAndRejected }
            };

            var expr = TicketFlowDefinitions.Instance.GetIsVisibleExpression(new TicketWorkFlowContext() { User = user });

            using (var db = new TicketsContext(TicketsContext.EffortConnection()))
            {
                var q = db.Tickets.Where(expr);
                var sql = q.ToString();
                var tickets = q.ToList();
                Assert.Equal(2, tickets.Count());
                Assert.True(tickets.All(t => t.CurrentNodeID.In(TicketNodes.Approved, TicketNodes.Rejected)));
            }
        }

        [Fact]
        public void GetActionIsAllowedExpression()
        {
            var user = new User()
            {
                RoleIDs = { Roles.CannotViewTitleInPending }
            };

            var expr = TicketFlowDefinitions.Instance.GetActionIsAllowedExpression(
                TicketActions.Approve,
                new TicketWorkFlowContext() { User = user });

            var tickets = GetTickets();

            var expectedTickets = tickets.Where(x => x.CurrentNodeID == TicketNodes.Pending && x.Title != null);

            var filteredTickets = tickets.AsQueryable().Where(expr).ToList();
            Assert.Equal(expectedTickets.Count(), filteredTickets.Count());
            Assert.True(expectedTickets.All(et => filteredTickets.Contains(et)));
        }

        [Fact]
        public void GetActionIsAllowedExpression_NodeNotVisible()
        {
            var user = new User()
            {
                RoleIDs = { Roles.CannotViewPending }
            };

            var expr = TicketFlowDefinitions.Instance.GetActionIsAllowedExpression(
                TicketActions.Approve,
                new TicketWorkFlowContext() { User = user });

            var tickets = GetTickets();

            var filteredTickets = tickets.AsQueryable().Where(expr).ToList();
            Assert.Equal(0, filteredTickets.Count());
        }

        [Fact]
        public void GetActionIsAllowedExpression_EF()
        {
            var user = new User()
            {
                RoleIDs = { Roles.CannotViewTitleInPending }
            };

            var expr = TicketFlowDefinitions.Instance.GetActionIsAllowedExpression(
                TicketActions.Approve,
                new TicketWorkFlowContext() { User = user });

            using (var db = new TicketsContext(TicketsContext.EffortConnection()))
            {
                var q = db.Tickets.Where(expr);
                var sql = q.ToString();
                var tickets = q.ToList();
                Assert.Equal(1, tickets.Count());
                Assert.True(tickets.All(t => t.CurrentNodeID.In(TicketNodes.Pending) && t.Title != null));
            }
        }

        [Fact]
        public void GetActionIsAllowedExpression_WithoutContext()
        {
            var expr = TicketFlowDefinitions.Instance.GetActionIsAllowedExpression(TicketActions.Approve);

            var tickets = GetTickets();

            var expectedTickets = tickets.Where(x => x.CurrentNodeID == TicketNodes.Pending);

            var filteredTickets = tickets.AsQueryable().Where(expr).ToList();
            Assert.Equal(expectedTickets.Count(), filteredTickets.Count());
            Assert.True(expectedTickets.All(et => filteredTickets.Contains(et)));
        }

        [Fact]
        public void GetIsAnyActionAllowedExpression()
        {
            var user = new User()
            {
                RoleIDs = { Roles.CanClose }
            };

            var expr = TicketFlowDefinitions.Instance.GetIsAnyActionAllowedExpression(
                new TicketWorkFlowContext() { User = user });

            var tickets = GetTickets();

            var expectedTickets = tickets.Where(x =>
                x.CurrentNodeID == TicketNodes.Pending && x.Title != null
                || x.CurrentNodeID == TicketNodes.Approved
                || x.CurrentNodeID == TicketNodes.Rejected);

            var filteredTickets = tickets.AsQueryable().Where(expr).ToList();
            Assert.Equal(expectedTickets.Count(), filteredTickets.Count());
            Assert.True(expectedTickets.All(et => filteredTickets.Contains(et)));
        }

        [Fact]
        public void GetIsAnyActionAllowedExpression_NodeNotVisible()
        {
            var user = new User()
            {
                RoleIDs = { Roles.CanClose, Roles.CannotViewPending }
            };

            var expr = TicketFlowDefinitions.Instance.GetIsAnyActionAllowedExpression(
                new TicketWorkFlowContext() { User = user });

            var tickets = GetTickets();

            var expectedTickets = tickets.Where(x =>
                x.CurrentNodeID == TicketNodes.Approved
                || x.CurrentNodeID == TicketNodes.Rejected);

            var filteredTickets = tickets.AsQueryable().Where(expr).ToList();
            Assert.Equal(expectedTickets.Count(), filteredTickets.Count());
            Assert.True(expectedTickets.All(et => filteredTickets.Contains(et)));
        }

        [Fact]
        public void GetIsAnyActionAllowedExpression_EF()
        {
            var user = new User()
            {
                RoleIDs = { Roles.CanClose }
            };

            var expr = TicketFlowDefinitions.Instance.GetIsAnyActionAllowedExpression(
                new TicketWorkFlowContext() { User = user });

            using (var db = new TicketsContext(TicketsContext.EffortConnection()))
            {
                var q = db.Tickets.Where(expr);
                var sql = q.ToString();
                var tickets = q.ToList();
                Assert.Equal(3, tickets.Count());
                Assert.True(tickets.All(x =>
                    x.CurrentNodeID == TicketNodes.Pending && x.Title != null
                    || x.CurrentNodeID == TicketNodes.Approved
                    || x.CurrentNodeID == TicketNodes.Rejected));
            }
        }
    }
}