using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Bardock.Flowont.Tests
{
    public enum TicketActions
    {
        Approve = 1,
        Reject = 2,
        Close = 3,
        Archive = 4,
        UndoApprove = 5,
        Stay = 6
    }

    public enum TicketNodes
    {
        Pending = 1,
        Approved = 2,
        Rejected = 3,
        Closed = 4,
        Archived = 5
    }

    public enum TicketFlows
    {
        v1 = 1,
        v2 = 2
    }

    public class Ticket : Entities.IMultiFlowEntity<TicketFlows>
    {
        public TicketFlows FlowID { get; set; }

        public TicketNodes CurrentNodeID { get; set; }

        public virtual ICollection<TicketPathLog> PathLogs { get; set; }

        public int ID { get; set; }

        public string Title { get; set; }

        public DateTime CreatedOn { get; set; }

        public Ticket()
        {
            this.PathLogs = new HashSet<TicketPathLog>();
        }
    }

    public class TicketPathLog
    {
        public int ID { get; set; }

        public int TicketID { get; set; }

        public TicketActions? ActionID { get; set; }

        public TicketNodes? FromNodeID { get; set; }

        public TicketNodes ToNodeID { get; set; }

        public DateTime CreatedOn { get; set; }
    }

    public class TicketStateHandler : Entities.IFlowEntityStateHandler<Ticket, TicketNodes, TicketActions, TicketWorkFlowContext>
    {
        protected static TicketStateHandler _instance = new TicketStateHandler();

        public static TicketStateHandler Instance
        {
            get { return _instance; }
        }

        protected TicketStateHandler()
        {
        }

        public Expression<Func<Ticket, bool>> GetIsInNodeExpression(TicketNodes nodeID)
        {
            return t => t.CurrentNodeID == nodeID;
        }

        public IList<TicketNodes> GetCurrentNodeIDs(Ticket e)
        {
            return new List<TicketNodes>() { e.CurrentNodeID };
        }

        public void SetCurrentNodeIDs(Ticket e, IList<TicketNodes> ids)
        {
            if (ids.Count() != 1)
            {
                throw new InvalidOperationException(string.Format("{0} node IDs were specified. Entity can only have one node.", ids.Count()));
            }
            e.CurrentNodeID = ids.First();
        }

        public void OnTransitions(Ticket e, IList<Entities.PathLog<TicketNodes, TicketActions>> logs, TicketWorkFlowContext ctx)
        {
            logs.Select(x => new TicketPathLog()
                {
                    ActionID = x.ActionID,
                    FromNodeID = x.FromNodeID,
                    ToNodeID = x.ToNodeID,
                    CreatedOn = DateTime.UtcNow
                })
                .ToList()
                .ForEach(x => e.PathLogs.Add(x));
        }
    }

    public enum Roles
    {
        CanViewPending,
        CannotViewPending,
        OnlyApprovedAndRejected,
        CannotViewTitleInPending,
        CannotEditCreatedOnInPending,
        CanClose,
        CanArchive,
        CanUndoApprove,
        CanReject
    }

    public class TicketWorkFlowContext
    {
        public User User { get; set; }

        public TicketsContext Db { get; set; }

        public Action OnPendingEntry { get; set; }

        public Action OnPendingExit { get; set; }

        public Action OnApprovedEntry { get; set; }

        public Action OnRejectedEntry { get; set; }
    }

    public class User
    {
        public IList<Roles> RoleIDs { get; set; }
    }

    public class TicketFlow : Flow<TicketNodes, TicketActions, Ticket, TicketWorkFlowContext>
    {
        public TicketFlow()
            : base(TicketStateHandler.Instance)
        {
            this.AddNode(TicketNodes.Pending, isInitial: true)
                .OnEntry((t, logs, ctx) => { ctx.OnPendingEntry(); })
                .IsVisibleWhen((t, ctx) => !ctx.User.RoleIDs.Contains(Roles.CannotViewPending) && !ctx.User.RoleIDs.Contains(Roles.OnlyApprovedAndRejected))
                .IsVisibleWhen((t, ctx) => !ctx.User.RoleIDs.Contains(Roles.CannotViewTitleInPending), forField: t => t.Title)
                .IsEditableWhen((t, ctx) => !ctx.User.RoleIDs.Contains(Roles.CannotViewPending))
                .IsEditableWhen((t, ctx) => !ctx.User.RoleIDs.Contains(Roles.CannotEditCreatedOnInPending), forField: t => t.CreatedOn)
                .Permit(TicketActions.Approve, c => c
                    .IsHappyPath()
                    .When((t, ctx) => t.Title != null)
                    .Then((t, ctx) => t.Title += "_approved")
                    .TransitionTo(TicketNodes.Approved)
                )
                .Permit(TicketActions.Reject, c => c
                    .When((t, ctx) => ctx.User.RoleIDs.Contains(Roles.CanReject))
                    .TransitionTo(TicketNodes.Rejected)
                )
                .OnExit((t, logs, ctx) => { ctx.OnPendingExit(); });

            this.AddNode(TicketNodes.Approved)
                .OnEntry((t, logs, ctx) => { ctx.OnApprovedEntry(); })
                .Permit(TicketActions.Stay, c => c
                    .IsHappyPath()
                    .TransitionTo(TicketNodes.Approved)
                )
                .Permit(TicketActions.Close, c => c
                    .IsHappyPath()
                    .When((t, ctx) => ctx.User.RoleIDs.Contains(Roles.CanClose))
                    .TransitionTo(TicketNodes.Closed)
                )
                .Permit(TicketActions.UndoApprove, c => c
                    .When((t, ctx) => ctx.User.RoleIDs.Contains(Roles.CanUndoApprove))
                    .TransitionTo(TicketNodes.Pending)
                );

            this.AddNode(TicketNodes.Rejected)
                .OnEntry((t, logs, ctx) => { ctx.OnRejectedEntry(); })
                .IsVisibleWhen((t, ctx) => ctx.User.RoleIDs.Any())
                .Permit(TicketActions.Close, c => c
                    .When((t, ctx) => ctx.User.RoleIDs.Contains(Roles.CanClose))
                    .TransitionTo(TicketNodes.Closed)
                );

            this.AddNode(TicketNodes.Closed)
                .IsVisibleWhen((t, ctx) => !ctx.User.RoleIDs.Contains(Roles.OnlyApprovedAndRejected))
                .Permit(TicketActions.Archive, c => c
                    .IsHappyPath()
                    .When((t, ctx) => ctx.User.RoleIDs.Contains(Roles.CanArchive))
                    .TransitionTo(TicketNodes.Archived)
                );

            this.AddNode(TicketNodes.Archived)
                .IsVisibleWhen((t, ctx) => !ctx.User.RoleIDs.Contains(Roles.OnlyApprovedAndRejected));
        }
    }

    public class TicketFlowDefinitions : FlowDefinitions<TicketFlows, TicketNodes, TicketActions, Ticket, TicketWorkFlowContext>
    {
        protected static TicketFlowDefinitions _instance = new TicketFlowDefinitions();

        public static TicketFlowDefinitions Instance
        {
            get { return _instance; }
        }

        protected TicketFlowDefinitions()
            : base()
        {
            this.Flows[TicketFlows.v1] = new TicketFlow();
            this.DefaultFlowID = TicketFlows.v1;
        }
    }

    public class TicketWorkflowEngine : WorkflowEngine<TicketFlows, TicketNodes, TicketActions, Ticket, TicketWorkFlowContext>
    {
        public TicketWorkflowEngine(Ticket entity, TicketWorkFlowContext ctx = null)
            : base(TicketFlowDefinitions.Instance, entity, TicketStateHandler.Instance, ctx) { }
    }

    public static class TicketQueryExtensions
    {
        public static IQueryable<Ticket> WhereIsInNode(this IQueryable<Ticket> source, TicketNodes node)
        {
            return source.WhereIsInNode(node, TicketFlowDefinitions.Instance);
        }

        public static IQueryable<Ticket> WhereIsVisible(this IQueryable<Ticket> source, TicketWorkFlowContext ctx)
        {
            return source.WhereIsVisible(ctx, TicketFlowDefinitions.Instance);
        }

        public static IQueryable<Ticket> WhereActionIsAllowed(this IQueryable<Ticket> source, TicketActions actionID, TicketWorkFlowContext ctx)
        {
            return source.WhereActionIsAllowed(actionID, ctx, TicketFlowDefinitions.Instance);
        }
    }
}