using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Bardock.Flowont.Tests
{
    public class WorkflowEngineTest
    {
        [Fact]
        public void InitializeEntity_FlowMissing()
        {
            var e = new Ticket()
            {
                CurrentNodeID = TicketNodes.Pending
            };
            var onPendingEntry = 0;
            var ctx = new TicketWorkFlowContext()
            {
                User = new User(),
                OnPendingEntry = () => { onPendingEntry++; }
            };

            var workflow = new TicketWorkflowEngine(e, ctx).InitializeEntity();
            Assert.Equal(TicketFlowDefinitions.Instance.GetFlow(TicketFlows.v1), workflow.Flow);
            Assert.Equal(TicketFlows.v1, e.FlowID);
            Assert.Equal(1, e.PathLogs.Count());
            Assert.Equal(1, onPendingEntry);
        }

        [Fact]
        public void InitializeEntity_CurrentNodesMissing()
        {
            var e = new Ticket()
            {
                FlowID = TicketFlows.v1
            };
            var onPendingEntry = 0;
            var ctx = new TicketWorkFlowContext()
            {
                User = new User(),
                OnPendingEntry = () => { onPendingEntry++; }
            };

            var workflow = new TicketWorkflowEngine(e, ctx).InitializeEntity();
            Assert.Equal(1, workflow.CurrentNodes.Count());
            Assert.Equal(TicketNodes.Pending, workflow.CurrentNodes.First().ID);
            Assert.Equal(TicketNodes.Pending, e.CurrentNodeID);
            Assert.Equal(1, e.PathLogs.Count());
            Assert.Equal(1, onPendingEntry);
        }

        [Fact]
        public void UnexistingFlow()
        {
            var e = new Ticket()
            {
                FlowID = (TicketFlows)int.MaxValue
            };
            Assert.Throws<ArgumentException>(() => new TicketWorkflowEngine(e));
        }

        [Fact]
        public void NodeIsVisible()
        {
            var e = new Ticket()
            {
                FlowID = TicketFlows.v1,
                CurrentNodeID = TicketNodes.Pending
            };
            var user = new User()
            {
                RoleIDs = new List<Roles>() { Roles.CanViewPending }
            };
            var workflow = new TicketWorkflowEngine(e);

            Assert.True(workflow.IsVisible(new TicketWorkFlowContext() { User = user }));
        }

        [Fact]
        public void NodeIsNotVisible()
        {
            var e = new Ticket()
            {
                FlowID = TicketFlows.v1,
                CurrentNodeID = TicketNodes.Pending
            };
            var user = new User()
            {
                RoleIDs = new List<Roles>() { Roles.CannotViewPending }
            };
            var workflow = new TicketWorkflowEngine(e);

            Assert.False(workflow.IsVisible(new TicketWorkFlowContext() { User = user }));
        }

        [Fact]
        public void FieldIsVisible()
        {
            var e = new Ticket()
            {
                FlowID = TicketFlows.v1,
                CurrentNodeID = TicketNodes.Pending
            };
            var user = new User()
            {
                RoleIDs = new List<Roles>() { Roles.CanViewPending }
            };
            var workflow = new TicketWorkflowEngine(e, new TicketWorkFlowContext() { User = user });

            Assert.True(workflow.IsVisible(x => x.Title));
        }

        [Fact]
        public void FieldIsVisible_Unspecified()
        {
            var e = new Ticket()
            {
                FlowID = TicketFlows.v1,
                CurrentNodeID = TicketNodes.Pending
            };
            var user = new User()
            {
                RoleIDs = new List<Roles>() { Roles.CannotViewTitleInPending }
            };
            var workflow = new TicketWorkflowEngine(e, new TicketWorkFlowContext() { User = user });

            Assert.True(workflow.IsVisible(x => x.CreatedOn));
        }

        [Fact]
        public void FieldIsNotVisible()
        {
            var e = new Ticket()
            {
                FlowID = TicketFlows.v1,
                CurrentNodeID = TicketNodes.Pending
            };
            var user = new User()
            {
                RoleIDs = new List<Roles>() { Roles.CannotViewTitleInPending }
            };
            var workflow = new TicketWorkflowEngine(e, new TicketWorkFlowContext() { User = user });

            Assert.False(workflow.IsVisible(x => x.Title));
        }

        [Fact]
        public void FieldIsEditable()
        {
            var e = new Ticket()
            {
                FlowID = TicketFlows.v1,
                CurrentNodeID = TicketNodes.Pending
            };
            var user = new User()
            {
                RoleIDs = new List<Roles>() { Roles.CanViewPending }
            };
            var workflow = new TicketWorkflowEngine(e, new TicketWorkFlowContext() { User = user });

            Assert.True(workflow.IsEditable(x => x.CreatedOn));
        }

        [Fact]
        public void FieldIsEditable_Unspecified()
        {
            var e = new Ticket()
            {
                FlowID = TicketFlows.v1,
                CurrentNodeID = TicketNodes.Pending
            };
            var user = new User()
            {
                RoleIDs = new List<Roles>() { Roles.CannotEditCreatedOnInPending }
            };
            var workflow = new TicketWorkflowEngine(e, new TicketWorkFlowContext() { User = user });

            Assert.True(workflow.IsEditable(x => x.Title));
        }

        [Fact]
        public void FieldIsNotEditable()
        {
            var e = new Ticket()
            {
                FlowID = TicketFlows.v1,
                CurrentNodeID = TicketNodes.Pending
            };
            var user = new User()
            {
                RoleIDs = new List<Roles>() { Roles.CannotEditCreatedOnInPending }
            };
            var workflow = new TicketWorkflowEngine(e, new TicketWorkFlowContext() { User = user });

            Assert.False(workflow.IsEditable(x => x.CreatedOn));
        }

        [Fact]
        public void ActionIsAllowed()
        {
            using (var db = new TicketsContext(TicketsContext.EffortConnection()))
            {
                var e = db.Tickets.WhereIsInNode(TicketNodes.Pending).First();

                var ctx = new TicketWorkFlowContext()
                {
                    User = new User()
                    {
                        RoleIDs = new List<Roles>() { Roles.CannotEditCreatedOnInPending }
                    }
                };

                var workflow = new TicketWorkflowEngine(e);

                Assert.True(workflow.ActionIsAllowed(TicketActions.Approve, ctx));
            }
        }

        [Fact]
        public void ActionIsAllowed_NotVisibleNode()
        {
            using (var db = new TicketsContext(TicketsContext.EffortConnection()))
            {
                var e = db.Tickets.WhereIsInNode(TicketNodes.Pending).First();

                var ctx = new TicketWorkFlowContext()
                {
                    User = new User()
                    {
                        RoleIDs = new List<Roles>() { Roles.CannotViewPending }
                    }
                };

                var workflow = new TicketWorkflowEngine(e);

                Assert.False(workflow.ActionIsAllowed(TicketActions.Approve, ctx));
            }
        }

        [Fact]
        public void ActionIsAllowed_UnexistingEdge()
        {
            using (var db = new TicketsContext(TicketsContext.EffortConnection()))
            {
                var e = db.Tickets.WhereIsInNode(TicketNodes.Pending).First();

                var ctx = new TicketWorkFlowContext()
                {
                    User = new User()
                    {
                        RoleIDs = new List<Roles>() { Roles.CannotEditCreatedOnInPending }
                    }
                };

                var workflow = new TicketWorkflowEngine(e);

                Assert.False(workflow.ActionIsAllowed(TicketActions.Close, ctx));
            }
        }

        [Fact]
        public void ActionIsAllowed_NotSatisfiedRule()
        {
            using (var db = new TicketsContext(TicketsContext.EffortConnection()))
            {
                var e = db.Tickets.WhereIsInNode(TicketNodes.Pending).First();

                var ctx = new TicketWorkFlowContext()
                {
                    User = new User()
                    {
                        RoleIDs = new List<Roles>() { Roles.CannotEditCreatedOnInPending }
                    }
                };

                e.Title = null;
                var workflow = new TicketWorkflowEngine(e);

                Assert.False(workflow.ActionIsAllowed(TicketActions.Approve, ctx));
            }
        }

        [Fact]
        public void Eval()
        {
            using (var db = new TicketsContext(TicketsContext.EffortConnection()))
            {
                var e = db.Tickets.WhereIsInNode(TicketNodes.Pending).First();
                var originalPathLogs = e.PathLogs.ToList();

                var onPendingEntry = 0;
                var onPendingExit = 0;
                var onApprovedEntry = 0;
                var onRejectedEntry = 0;

                var ctx = new TicketWorkFlowContext()
                {
                    User = new User()
                    {
                        RoleIDs = new List<Roles>() { Roles.CannotEditCreatedOnInPending }
                    },
                    OnPendingEntry = () => { onPendingEntry++; },
                    OnPendingExit = () => { onPendingExit++; },
                    OnApprovedEntry = () => { onApprovedEntry++; },
                    OnRejectedEntry = () => { onRejectedEntry++; }
                };

                var workflow = new TicketWorkflowEngine(e);
                Assert.Equal(0, workflow.CurrentNodes.Count(x => x.ID == TicketNodes.Approved));

                workflow.Eval(TicketActions.Approve, ctx);

                db.SaveChanges();
                e = db.Tickets.Where(x => x.ID == e.ID).First();

                Assert.Equal(TicketNodes.Approved, e.CurrentNodeID);
                Assert.Equal(1, workflow.CurrentNodes.Count(x => x.ID == TicketNodes.Approved));
                Assert.True(e.Title.EndsWith("_approved"));

                Assert.Equal(originalPathLogs.Count() + 1, e.PathLogs.Count());
                var log = e.PathLogs.First(x => !originalPathLogs.Contains(x));
                Assert.Equal(TicketActions.Approve, log.ActionID);
                Assert.Equal(TicketNodes.Pending, log.FromNodeID);
                Assert.Equal(TicketNodes.Approved, log.ToNodeID);

                Assert.Equal(0, onPendingEntry);
                Assert.Equal(1, onPendingExit);
                Assert.Equal(1, onApprovedEntry);
                Assert.Equal(0, onRejectedEntry);
            }
        }

        [Fact]
        public void Eval_NotVisibleNode()
        {
            using (var db = new TicketsContext(TicketsContext.EffortConnection()))
            {
                var e = db.Tickets.WhereIsInNode(TicketNodes.Pending).First();

                var user = new User()
                {
                    RoleIDs = new List<Roles>() { Roles.CannotViewPending }
                };

                var workflow = new TicketWorkflowEngine(e);

                Assert.Throws<ActionIsNotAllowedException>(() =>
                    workflow.Eval(TicketActions.Approve, new TicketWorkFlowContext() { User = user }));
            }
        }

        [Fact]
        public void Eval_UnexistingEdge()
        {
            using (var db = new TicketsContext(TicketsContext.EffortConnection()))
            {
                var e = db.Tickets.WhereIsInNode(TicketNodes.Pending).First();

                var user = new User()
                {
                    RoleIDs = new List<Roles>() { Roles.CannotEditCreatedOnInPending }
                };

                var workflow = new TicketWorkflowEngine(e);

                Assert.Throws<ActionIsNotAllowedException>(() =>
                    workflow.Eval(TicketActions.Close, new TicketWorkFlowContext() { User = user }));
            }
        }

        [Fact]
        public void Eval_NotSatisfiedRule()
        {
            using (var db = new TicketsContext(TicketsContext.EffortConnection()))
            {
                var e = db.Tickets.WhereIsInNode(TicketNodes.Pending).First();

                var user = new User()
                {
                    RoleIDs = new List<Roles>() { Roles.CannotEditCreatedOnInPending }
                };

                e.Title = null;
                var workflow = new TicketWorkflowEngine(e);

                Assert.Throws<ActionIsNotAllowedException>(() =>
                    workflow.Eval(TicketActions.Approve, new TicketWorkFlowContext() { User = user }));
            }
        }
    }
}