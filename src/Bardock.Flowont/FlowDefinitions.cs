using Bardock.Utils.Extensions;
using LinqKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Bardock.Flowont
{
    /// <summary>
    /// Defines existing Flow versions for an entity type
    /// </summary>
    public abstract class FlowDefinitions<TFlowID, TNodeID, TActionID, TEntity, TContext>
        where TNodeID : struct, IConvertible
        where TActionID : struct, IConvertible
        where TEntity : class, Entities.IMultiFlowEntity<TFlowID>
    {
        public IDictionary<TFlowID, Flow<TNodeID, TActionID, TEntity, TContext>> Flows { get; protected set; }

        public TFlowID DefaultFlowID { get; protected set; }

        public FlowDefinitions()
        {
            this.Flows = new Dictionary<TFlowID, Flow<TNodeID, TActionID, TEntity, TContext>>();
        }

        public Flow<TNodeID, TActionID, TEntity, TContext> GetFlow(TFlowID id)
        {
            if (EqualityComparer<TFlowID>.Default.Equals(id, default(TFlowID)))
            {
                id = DefaultFlowID;
            }
            if (!Flows.ContainsKey(id))
            {
                throw new ArgumentException("Does not exists a Flow with specified ID");
            }
            return Flows[id];
        }

        public Flow<TNodeID, TActionID, TEntity, TContext> GetFlow(TEntity e)
        {
            return GetFlow(e.FlowID);
        }

        public Flow<TNodeID, TActionID, TEntity, TContext> GetDefaultFlow()
        {
            return GetFlow(this.DefaultFlowID);
        }

        public TFlowID GetFlowID(Flow<TNodeID, TActionID, TEntity, TContext> flow)
        {
            if (!Flows.Any(x => x.Value == flow))
                throw new ArgumentException("Specified flows does not exist");
            return Flows.First(x => x.Value == flow).Key;
        }

        /// <summary>
        /// Returns an expression that evaluates if a given entity is in specified node
        /// </summary>
        public Expression<Func<TEntity, bool>> GetIsInNodeExpression(TNodeID nodeID)
        {
            return GetDefaultFlow().GetIsInNodeExpression(nodeID);
        }

        /// <summary>
        /// Returns an expression that evaluates if a given entity is visible by specified context
        /// </summary>
        public virtual Expression<Func<TEntity, bool>> GetIsVisibleExpression(TContext ctx)
        {
            return AggregateExpression(flow => flow.GetIsVisibleExpression(ctx));
        }

        /// <summary>
        /// Returns an expression that evaluates if specified action is allowed in a given entity and specified context
        /// </summary>
        public Expression<Func<TEntity, bool>> GetActionIsAllowedExpression(TActionID actionID, TContext ctx)
        {
            return AggregateExpression(flow => flow.GetActionIsAllowedExpression(actionID, ctx));
        }

        /// <summary>
        /// Returns an expression that evaluates if any specified action is allowed in a given entity and specified context
        /// </summary>
        public Expression<Func<TEntity, bool>> GetActionIsAllowedExpression(TActionID[] actionIDs, TContext ctx)
        {
            return AggregateExpression(flow => flow.GetActionIsAllowedExpression(actionIDs, ctx));
        }

        /// <summary>
        /// Returns an expression that evaluates if specified action is allowed in a given entity.
        /// It does not evaluates any rule that depends on a context, only nodes are taken into account.
        /// </summary>
        public Expression<Func<TEntity, bool>> GetActionIsAllowedExpression(params TActionID[] actionIDs)
        {
            return AggregateExpression(flow => flow.GetActionIsAllowedExpression(actionIDs));
        }

        /// <summary>
        /// Returns an expression that evaluates if a given entity has any action allowed
        /// </summary>
        public virtual Expression<Func<TEntity, bool>> GetIsAnyActionAllowedExpression(TContext ctx)
        {
            return AggregateExpression(flow => flow.GetIsAnyActionAllowedExpression(ctx));
        }

        /// <summary>
        /// Returns an expression that evaluates if a given entity has any action allowed
        /// </summary>
        public IEnumerable<TActionID> HappyPathActions
        {
            get
            {
                return this.Flows.SelectMany(x => x.Value.HappyPathActions).Distinct();
            }
        }

        /// <summary>
        /// Aggregates flow expressions
        /// </summary>
        /// <param name="exprFactory">A function that receives a Flow and return an expression</param>
        private Expression<Func<TEntity, bool>> AggregateExpression(
            Func<Flow<TNodeID, TActionID, TEntity, TContext>, Expression<Func<TEntity, bool>>> exprFactory)
        {
            var expr = Linq.Expr((TEntity x) => false);
            foreach (var flow in this.Flows)
            {
                var flowID = flow.Key;
                var flowExpr = exprFactory(flow.Value);
                if (flowExpr.IsConstant(false))
                    continue;
                expr = Linq.Expr((TEntity x) => expr.Invoke(x) || flowID.Equals(x.FlowID) && flowExpr.Invoke(x)).Expand();
            }
            return expr.ReplaceEqualsMethodByOperator();
        }
    }
}