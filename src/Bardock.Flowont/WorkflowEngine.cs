using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Bardock.Utils.Extensions;
using Bardock.Utils.Linq.Expressions;

namespace Bardock.Flowont
{
    /// <summary>
    /// Defines read-only methods of an entity workflow
    /// </summary>
    public interface IWorkflowEngine<TFlowID, TEntity>
        where TEntity : class, Entities.IMultiFlowEntity<TFlowID>
    {
        TEntity Entity { get; }

        bool IsVisible();

        bool IsVisible(Expression<Func<TEntity, object>> forField);

        bool IsEditable();

        bool IsEditable(Expression<Func<TEntity, object>> forField);
    }

    /// <summary>
    /// Represents an entity workflow instance.
    /// Defines methods to be evaluated to a specified entity and context.
    /// </summary>
    public class WorkflowEngine<TFlowID, TNodeID, TActionID, TEntity, TContext> : IWorkflowEngine<TFlowID, TEntity>
        where TEntity : class, Entities.IMultiFlowEntity<TFlowID>
        where TContext : class
        where TNodeID : struct, IConvertible
        where TActionID : struct, IConvertible
    {
        /// <summary>
        /// Flows definitions
        /// </summary>
        public FlowDefinitions<TFlowID, TNodeID, TActionID, TEntity, TContext> FlowDefinitions { get; protected set; }

        public Entities.IFlowEntityStateHandler<TEntity, TNodeID, TActionID, TContext> EntityStateHandler { get; protected set; }

        /// <summary>
        /// Current entity's flow
        /// </summary>
        public Flow<TNodeID, TActionID, TEntity, TContext> Flow { get; protected set; }

        public TEntity Entity { get; protected set; }

        public IList<Node<TNodeID, TActionID, TEntity, TContext>> CurrentNodes { get; protected set; }

        /// <summary>
        /// Context used by default for evaluate flow conditions
        /// </summary>
        public TContext Context { get; protected set; }

        public WorkflowEngine(
            FlowDefinitions<TFlowID, TNodeID, TActionID, TEntity, TContext> flowDefinitions,
            TEntity entity,
            Entities.IFlowEntityStateHandler<TEntity, TNodeID, TActionID, TContext> entityStateHandler,
            TContext context)
        {
            this.FlowDefinitions = flowDefinitions;
            this.Flow = flowDefinitions.GetFlow(entity);
            this.Entity = entity;
            this.EntityStateHandler = entityStateHandler;
            this.Context = context;
            if (this.EntityStateHandler.GetCurrentNodeIDs(this.Entity) != null)
            {
                InitCurrentNodes(this.EntityStateHandler.GetCurrentNodeIDs(this.Entity));
            }
        }

        protected virtual void InitCurrentNodes(IEnumerable<TNodeID> ids)
        {
            this.CurrentNodes = this.Flow.Nodes.Where(n => ids.Contains(n.ID)).ToList();
        }

        /// <summary>
        /// Set initial nodes
        /// </summary>
        public virtual WorkflowEngine<TFlowID, TNodeID, TActionID, TEntity, TContext> InitializeEntity(TContext ctx = null)
        {
            ctx = ctx ?? this.Context;

            this.Entity.FlowID = this.FlowDefinitions.GetFlowID(this.Flow);

            var newNodeIDs = this.Flow.InitialNodes.Select(n => n.ID).ToList();
            var logs = newNodeIDs.Select(id => new Entities.PathLog<TNodeID, TActionID>() { ToNodeID = id }).ToList();
            var enteringNodes = logs.GroupBy(x => x.ToNodeID).ToDictionary(x => x.Key, x => x.ToList());

            DoTransition(ctx, newNodeIDs, logs, exitingNodes: null, enteringNodes: enteringNodes);
            return this;
        }

        /// <summary>
        /// Determines if current entity is visible using the default context
        /// </summary>
        public virtual bool IsVisible()
        {
            return this.IsVisible(ctx: null);
        }

        /// <summary>
        /// Determines if current entity is visible using specified context
        /// </summary>
        public virtual bool IsVisible(TContext ctx)
        {
            ctx = ctx ?? this.Context;
            if (ctx == null)
                throw new ArgumentNullException("ctx");

            return this.CurrentNodes.Any(n => n.IsVisibleExpr.Compile().Invoke(this.Entity, ctx));
        }

        /// <summary>
        /// Determines if current entity's field is visible using the default context
        /// </summary>
        public virtual bool IsVisible(Expression<Func<TEntity, object>> forField)
        {
            return this.IsVisible(forField, ctx: null);
        }

        /// <summary>
        /// Determines if current entity's field is visible using the specified context
        /// </summary>
        public virtual bool IsVisible(Expression<Func<TEntity, object>> forField, TContext ctx)
        {
            ctx = ctx ?? this.Context;
            if (ctx == null)
                throw new ArgumentNullException("ctx");

            var criterias = this.CurrentNodes
                .Where(n => n.FieldIsVisibleExprs.ContainsKey(ExpressionHelper.GetExpressionText(forField)))
                .Select(n => n.FieldIsVisibleExprs[ExpressionHelper.GetExpressionText(forField)])
                .ToList();
            var notVisibleInAny = criterias.Any(c => c.Compile().Invoke(this.Entity, ctx) == false);
            return !notVisibleInAny;
        }

        /// <summary>
        /// Determines if current entity is editable using the default context
        /// </summary>
        public virtual bool IsEditable()
        {
            return this.IsEditable(ctx: null);
        }

        /// <summary>
        /// Determines if current entity is visible using the specified context
        /// </summary>
        public virtual bool IsEditable(TContext ctx)
        {
            ctx = ctx ?? this.Context;
            if (ctx == null)
                throw new ArgumentNullException("ctx");

            return this.CurrentNodes.Any(n => n.IsEditableExpr.Compile().Invoke(this.Entity, ctx));
        }

        /// <summary>
        /// Determines if current entity's field is visible using the default context
        /// </summary>
        public virtual bool IsEditable(Expression<Func<TEntity, object>> forField)
        {
            return this.IsEditable(forField, ctx: null);
        }

        /// <summary>
        /// Determines if current entity's field is visible using the specified context
        /// </summary>
        public virtual bool IsEditable(Expression<Func<TEntity, object>> forField, TContext ctx = null)
        {
            ctx = ctx ?? this.Context;
            if (ctx == null)
                throw new ArgumentNullException("ctx");

            var criterias = this.CurrentNodes
                .Where(n => n.FieldIsEditableExprs.ContainsKey(ExpressionHelper.GetExpressionText(forField)))
                .Select(n => n.FieldIsEditableExprs[ExpressionHelper.GetExpressionText(forField)])
                .ToList();
            var notEditableInAny = criterias.Any(c => c.Compile().Invoke(this.Entity, ctx) == false);
            return !notEditableInAny;
        }

        /// <summary>
        /// Determines if specified action is allowed in current entity using the default context
        /// </summary>
        public virtual bool ActionIsAllowed(TActionID actionID)
        {
            return this.ActionIsAllowed(actionID, ctx: null);
        }

        /// <summary>
        /// Determines if specified action is allowed in current entity using the specified context
        /// </summary>
        public virtual bool ActionIsAllowed(TActionID actionID, TContext ctx)
        {
            ctx = ctx ?? this.Context;
            return this.GetNodesWithAllowedEdges(ctx, actionID).Any();
        }

        /// <summary>
        /// Returns the list of allowed actions for the current entity using the specified context
        /// </summary>
        public IEnumerable<TActionID> GetAllowedActions(TContext ctx = null)
        {
            ctx = ctx ?? this.Context;

            if (ctx == null)
            {
                return this.CurrentNodes
                    .SelectMany(n => n.Edges)
                    .Select(e => e.ActionID);
            }

            return GetNodesWithAllowedEdges(ctx)
                .SelectMany(n => n.Value)
                .Select(e => e.ActionID);
        }

        /// <summary>
        /// Returns the list of nodes that the current entity would do a transition if given action is evaluated using the specified context
        /// </summary>
        public IEnumerable<TNodeID> GetDestinationNodes(TActionID action, TContext ctx = null)
        {
            return GetNodesWithAllowedEdges(ctx, action)
                .SelectMany(n => n.Value)
                .Select(e => e.NodeToID)
                .Distinct();
        }

        /// <summary>
        /// Evaluates/executes an action in current entity
        /// </summary>
        public virtual void Eval(TActionID actionID, TContext ctx = null)
        {
            ctx = ctx ?? this.Context;
            if (ctx == null)
                throw new ArgumentNullException("ctx");

            var sourceNodes = GetNodesWithAllowedEdges(ctx, actionID);
            if (!sourceNodes.Any())
            {
                throw new ActionIsNotAllowedException();
                // TODO: improve error message
            }

            if (this.Flow.Nodes.Any(n => !n.EdgesAreExclusive))
            {
                // TODO check that all incoming edges at each node are satisfied
                // and action is not already evaluated
                throw new NotImplementedException("Transition is not implemented for flows with non-exclusive nodes");
            }

            var newNodeIDs = this.CurrentNodes
                .Where(x => !sourceNodes.Keys.Contains(x) || !x.EdgesAreExclusive)
                .Select(x => x.ID)
                .ToList();

            var logs = new List<Entities.PathLog<TNodeID, TActionID>>();
            var exitingNodes = new Dictionary<Node<TNodeID, TActionID, TEntity, TContext>, List<Entities.PathLog<TNodeID, TActionID>>>();
            var enteringNodes = new Dictionary<TNodeID, List<Entities.PathLog<TNodeID, TActionID>>>();

            foreach (var sourceNode in sourceNodes)
            {
                EvalNode(sourceNode, ctx, newNodeIDs, logs, exitingNodes, enteringNodes);
            }

            DoTransition(ctx, newNodeIDs, logs, exitingNodes, enteringNodes);
        }

        protected virtual void DoTransition(
            TContext ctx,
            List<TNodeID> newNodeIDs,
            List<Entities.PathLog<TNodeID, TActionID>> logs,
            Dictionary<Node<TNodeID, TActionID, TEntity, TContext>, List<Entities.PathLog<TNodeID, TActionID>>> exitingNodes,
            Dictionary<TNodeID, List<Entities.PathLog<TNodeID, TActionID>>> enteringNodes)
        {
            this.EntityStateHandler.SetCurrentNodeIDs(this.Entity, newNodeIDs);
            InitCurrentNodes(newNodeIDs);

            this.EntityStateHandler.OnTransitions(this.Entity, logs, ctx);

            if (exitingNodes != null)
            {
                foreach (var exitingNode in exitingNodes)
                {
                    var node = exitingNode.Key;
                    var nodeLogs = exitingNode.Value;
                    foreach (var onExit in node.OnExit)
                    {
                        onExit(this.Entity, nodeLogs, ctx);
                    }
                }
            }

            foreach (var enteringNode in enteringNodes)
            {
                var nodeID = enteringNode.Key;
                var nodeLogs = enteringNode.Value;
                var node = this.CurrentNodes.First(n => n.ID.Equals(nodeID));
                foreach (var onEntry in node.OnEntry)
                {
                    onEntry(this.Entity, nodeLogs, ctx);
                }
            }
        }

        protected virtual Dictionary<Node<TNodeID, TActionID, TEntity, TContext>, IList<Edge<TNodeID, TActionID, TEntity, TContext>>> 
            GetNodesWithAllowedEdges(TContext ctx, TActionID? actionID = null)
        {
            ctx = ctx ?? this.Context;
            if (ctx == null)
                throw new ArgumentNullException("ctx");

            var visibleNodesWithEdges = this.CurrentNodes
                .Where(n => n.IsVisibleExpr.CompileCached().Invoke(this.Entity, ctx))
                .Select(n => new { Node = n, AllowedEdges = GetAllowedEdges(n, ctx, actionID) })
                .ToList();

            return visibleNodesWithEdges
                .Where(x => x.AllowedEdges.Any())
                .ToDictionary(x => x.Node, x => x.AllowedEdges);
        }

        protected virtual IList<Edge<TNodeID, TActionID, TEntity, TContext>> GetAllowedEdges(
            Node<TNodeID, TActionID, TEntity, TContext> node, TContext ctx, TActionID? actionID = null)
        {
            return node.Edges
                .Where(when: actionID != null, predicate: e => e.ActionID.Equals(actionID))
                .Where(e => e.Rules.All(r => r.CompileCached().Invoke(this.Entity, ctx)))
                .ToList();
        }

        protected virtual void EvalNode(
            KeyValuePair<Node<TNodeID, TActionID, TEntity, TContext>, IList<Edge<TNodeID, TActionID, TEntity, TContext>>> sourceNode,
            TContext ctx,
            List<TNodeID> newNodeIDs,
            List<Entities.PathLog<TNodeID, TActionID>> logs,
            Dictionary<Node<TNodeID, TActionID, TEntity, TContext>,
            List<Entities.PathLog<TNodeID, TActionID>>> exitingNodes,
            Dictionary<TNodeID, List<Entities.PathLog<TNodeID, TActionID>>> enteringNodes)
        {
            var sourceNodeLogs = new List<Entities.PathLog<TNodeID, TActionID>>();
            var anyLoop = false;

            foreach (var edge in sourceNode.Value)
            {
                EvalEdge(sourceNode, edge, ctx, newNodeIDs, enteringNodes, sourceNodeLogs, ref anyLoop);
            }
            logs.AddRange(sourceNodeLogs);

            if (!anyLoop)
            {
                exitingNodes.Add(sourceNode.Key, sourceNodeLogs);
            }
        }

        protected virtual void EvalEdge(
            KeyValuePair<Node<TNodeID, TActionID, TEntity, TContext>, IList<Edge<TNodeID, TActionID, TEntity, TContext>>> sourceNode,
            Edge<TNodeID, TActionID, TEntity, TContext> edge,
            TContext ctx,
            List<TNodeID> newNodeIDs,
            Dictionary<TNodeID, List<Entities.PathLog<TNodeID, TActionID>>> enteringNodes,
            List<Entities.PathLog<TNodeID, TActionID>> sourceNodeLogs,
            ref bool anyLoop)
        {
            foreach (var preProcess in edge.PreProcesses)
            {
                preProcess.Invoke(this.Entity, ctx);
            }
            if (!newNodeIDs.Contains(edge.NodeToID))
            {
                newNodeIDs.Add(edge.NodeToID);
            }
            var log = new Entities.PathLog<TNodeID, TActionID>()
            {
                ActionID = edge.ActionID,
                FromNodeID = sourceNode.Key.ID,
                ToNodeID = edge.NodeToID
            };
            sourceNodeLogs.Add(log);
            if (sourceNode.Key.ID.Equals(edge.NodeToID))
            {
                anyLoop = true;
            }
            else
            {
                if (!enteringNodes.ContainsKey(edge.NodeToID))
                    enteringNodes[edge.NodeToID] = new List<Entities.PathLog<TNodeID, TActionID>>();
                enteringNodes[edge.NodeToID].Add(log);
            }
        }
    }

    /// <summary>
    /// Actions is not allowed because
    /// there is not any edge in current nodes for specified action,
    /// or any edge's rule is not satisfied
    /// or node is not visible by specified context
    /// </summary>
    public class ActionIsNotAllowedException : Exception { }
}