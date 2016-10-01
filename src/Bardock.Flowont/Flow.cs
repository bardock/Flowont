using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Bardock.Utils.Extensions;
using LinqKit;

namespace Bardock.Flowont
{
    /// <summary>
    /// Defines the graph of the workflow
    /// </summary>
    public abstract class Flow<TNodeID, TActionID, TEntity, TContext>
        where TNodeID : struct, IConvertible
        where TActionID : struct, IConvertible
        where TEntity : class, Entities.IFlowEntity
    {
        public Entities.IFlowEntityStateHandler<TEntity, TNodeID, TActionID, TContext> EntityStateHandler { get; protected set; }

        public IList<Node<TNodeID, TActionID, TEntity, TContext>> Nodes { get; private set; }

        public IList<Node<TNodeID, TActionID, TEntity, TContext>> InitialNodes { get; private set; }

        public Flow(Entities.IFlowEntityStateHandler<TEntity, TNodeID, TActionID, TContext> entityStateHandler)
        {
            this.EntityStateHandler = entityStateHandler;
            this.Nodes = new List<Node<TNodeID, TActionID, TEntity, TContext>>();
            this.InitialNodes = new List<Node<TNodeID, TActionID, TEntity, TContext>>();
        }

        public IEnumerable<TActionID> HappyPathActions
        {
            get
            {
                return this.Nodes
                    .SelectMany(n => n.Edges)
                    .Where(e => e.IsHappyPath)
                    .Select(e => e.ActionID)
                    .Distinct();
            }
        }

        public IEnumerable<TNodeID> HappyPathStatus
        {
            get
            {
                var sortedNodes = new List<TNodeID>();
                var levelNodes = this.InitialNodes.AsEnumerable();

                while (levelNodes.Any())
                {
                    sortedNodes.AddRange(levelNodes.Select(n => n.ID));

                    levelNodes = levelNodes
                        .SelectMany(n => n.Edges)
                        .Where(e => e.IsHappyPath)
                        .Select(e => GetDestinationNode(e))
                        .Where(n => !sortedNodes.Contains(n.ID))
                        .ToList();
                }

                return sortedNodes;
            }
        }

        public IEnumerable<TNodeID> SortedNodes
        {
            get
            {
                var sortedNodes = HappyPathStatus;
                return sortedNodes
                    .Concat(this.Nodes
                        .Select(n => n.ID)
                        .Except(sortedNodes));
            }
        }

        private Node<TNodeID, TActionID, TEntity, TContext> GetDestinationNode(Edge<TNodeID, TActionID, TEntity, TContext> edge)
        {
            return this.Nodes.Single(n => n.ID.Equals(edge.NodeToID));
        }

        /// <summary>
        /// Adds a node and returns an associated NodeBuilder in order to be configured
        /// </summary>
        protected NodeBuilder<TNodeID, TActionID, TEntity, TContext> AddNode(TNodeID nodeID, bool isInitial = false)
        {
            var builder = new NodeBuilder<TNodeID, TActionID, TEntity, TContext>(nodeID);

            Nodes.Add(builder.Node);

            if (isInitial)
                InitialNodes.Add(builder.Node);

            return builder;
        }

        /// <summary>
        /// Returns an expression that evaluates if a given entity is in specified node
        /// </summary>
        public Expression<Func<TEntity, bool>> GetIsInNodeExpression(TNodeID nodeID)
        {
            return this.EntityStateHandler.GetIsInNodeExpression(nodeID);
        }

        /// <summary>
        /// Returns an expression that evaluates if a given entity is visible by specified context
        /// </summary>
        public Expression<Func<TEntity, bool>> GetIsVisibleExpression(TContext ctx)
        {
            var expr = Linq.Expr((TEntity x) => false);
            foreach (var node in this.Nodes)
            {
                var isInNodeExpr = GetIsInNodeExpression(node.ID);
                var isVisibleExpr = node.IsVisibleExpr.PartialApply(ctx).ReduceConstants();
                expr = Linq.Expr((TEntity x) => expr.Invoke(x) || isInNodeExpr.Invoke(x) && isVisibleExpr.Invoke(x)).Expand();
            }
            return expr.ReduceEvaluations();
        }

        /// <summary>
        /// Returns an expression that evaluates if specified action is allowed in a given entity and specified context
        /// </summary>
        public Expression<Func<TEntity, bool>> GetActionIsAllowedExpression(TActionID actionID, TContext ctx)
        {
            return GetIsAnyActionAllowedExpression(GetNodesWithAllowedEdges(actionID), ctx);
        }

        /// <summary>
        /// Returns an expression that evaluates if any specified action is allowed in a given entity and specified context
        /// </summary>
        public Expression<Func<TEntity, bool>> GetActionIsAllowedExpression(TActionID[] actionIDs, TContext ctx)
        {
            return GetIsAnyActionAllowedExpression(GetNodesWithAllowedEdges(actionIDs), ctx);
        }

        /// <summary>
        /// Returns an expression that evaluates if specified action is allowed in a given entity.
        /// It does not evaluates any specific context, only flow's edges.
        /// </summary>
        public Expression<Func<TEntity, bool>> GetActionIsAllowedExpression(params TActionID[] actionIDs)
        {
            return GetIsAnyActionAllowedExpression(GetNodesWithAllowedEdges(actionIDs));
        }

        /// <summary>
        /// Returns an expression that evaluates if a given entity has any action allowed
        /// </summary>
        public Expression<Func<TEntity, bool>> GetIsAnyActionAllowedExpression(TContext ctx)
        {
            return GetIsAnyActionAllowedExpression(GetNodesWithEdges(), ctx);
        }

        /// <param name="nodesWithEdges">A dictionary with a node as key and theirs edges to be evaluated as value</param>
        protected virtual Expression<Func<TEntity, bool>> GetIsAnyActionAllowedExpression(
            Dictionary<Node<TNodeID, TActionID, TEntity, TContext>, IEnumerable<Edge<TNodeID, TActionID, TEntity, TContext>>> nodesWithEdges,
            TContext ctx)
        {
            var isInAnyVisibleNodeThatSatisfiesAnyEdgeExpr = Linq.Expr((TEntity x) => false);
            foreach (var n in nodesWithEdges)
            {
                var node = n.Key;
                var edges = n.Value;

                var satisfiesAnyEdgeExpr = Linq.Expr((TEntity x) => false);
                foreach (var edge in edges)
                {
                    var satisfiesEdgeExpr = Linq.Expr((TEntity x) => true);
                    foreach (var rule in edge.Rules)
                    {
                        var ruleExpr = rule.PartialApply(ctx).ReduceConstants();
                        satisfiesEdgeExpr = Linq.Expr((TEntity x) => satisfiesEdgeExpr.Invoke(x) && ruleExpr.Invoke(x)).Expand();
                    }
                    satisfiesAnyEdgeExpr = Linq.Expr((TEntity x) => satisfiesAnyEdgeExpr.Invoke(x) || satisfiesEdgeExpr.Invoke(x)).Expand();
                }
                var isInNodeExpr = this.EntityStateHandler.GetIsInNodeExpression(node.ID);
                var isVisibleExpr = node.IsVisibleExpr.PartialApply(ctx).ReduceConstants();

                isInAnyVisibleNodeThatSatisfiesAnyEdgeExpr =
                    Linq.Expr((TEntity x) =>
                        isInAnyVisibleNodeThatSatisfiesAnyEdgeExpr.Invoke(x)
                        || isInNodeExpr.Invoke(x)
                            && isVisibleExpr.Invoke(x)
                            && satisfiesAnyEdgeExpr.Invoke(x))
                    .Expand();
            }
            return isInAnyVisibleNodeThatSatisfiesAnyEdgeExpr.ReduceEvaluations();
        }

        /// <summary>
        /// Returns an expression that evaluates if any of specified edges is allowed in a given entity.
        /// It does not evaluates any specific context, only flow's edges.
        /// </summary>
        /// <param name="nodesWithEdges">A dictionary with a node as key and theirs edges to be evaluated as value</param>
        protected virtual Expression<Func<TEntity, bool>> GetIsAnyActionAllowedExpression(
            Dictionary<Node<TNodeID, TActionID, TEntity, TContext>, IEnumerable<Edge<TNodeID, TActionID, TEntity, TContext>>> nodesWithEdges)
        {
            var isInAnyNodeExpr = Linq.Expr((TEntity x) => false);
            foreach (var node in nodesWithEdges.Keys)
            {
                var isInNodeExpr = this.EntityStateHandler.GetIsInNodeExpression(node.ID);
                isInAnyNodeExpr = Linq.Expr((TEntity x) => isInAnyNodeExpr.Invoke(x) || isInNodeExpr.Invoke(x)).Expand();
            }
            return isInAnyNodeExpr;
        }

        protected virtual Dictionary<Node<TNodeID, TActionID, TEntity, TContext>, IEnumerable<Edge<TNodeID, TActionID, TEntity, TContext>>> GetNodesWithEdges()
        {
            return this.Nodes
                .Select(n => new { Node = n, Edges = n.Edges })
                .Where(x => x.Edges.Any())
                .ToDictionary(x => x.Node, x => x.Edges.AsEnumerable());
        }

        protected virtual Dictionary<Node<TNodeID, TActionID, TEntity, TContext>, IEnumerable<Edge<TNodeID, TActionID, TEntity, TContext>>> GetNodesWithAllowedEdges(
            params TActionID[] actionIDs)
        {
            return this.Nodes
                .Select(n => new { Node = n, AllowedEdges = n.Edges.Where(e => actionIDs.Contains(e.ActionID)) })
                .Where(x => x.AllowedEdges.Any())
                .ToDictionary(x => x.Node, x => x.AllowedEdges);
        }
    }
}