using Bardock.Utils.Linq.Expressions;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Bardock.Flowont
{
    public class NodeBuilder<TNodeID, TActionID, TEntity, TContext>
        where TNodeID : struct, IConvertible
        where TActionID : struct, IConvertible
        where TEntity : class, Entities.IFlowEntity
    {
        public Node<TNodeID, TActionID, TEntity, TContext> Node { get; protected set; }

        public NodeBuilder(TNodeID id)
        {
            this.Node = new Node<TNodeID, TActionID, TEntity, TContext>()
            {
                ID = id
            };
        }

        /// <summary>
        /// Adds a function to be executed when the entity enters in the node
        /// </summary>
        public NodeBuilder<TNodeID, TActionID, TEntity, TContext> OnEntry(
            Action<TEntity, IList<Entities.PathLog<TNodeID, TActionID>>, TContext> onEntry)
        {
            this.Node.OnEntry.Add(onEntry);
            return this;
        }

        /// <summary>
        /// Adds a function to be executed when the entity goes out of the node
        /// </summary>
        public NodeBuilder<TNodeID, TActionID, TEntity, TContext> OnExit(
            Action<TEntity, IList<Entities.PathLog<TNodeID, TActionID>>, TContext> onExit)
        {
            this.Node.OnExit.Add(onExit);
            return this;
        }

        /// <summary>
        /// Adds an expressions that determine if an entity is visible by specified context
        /// </summary>
        public NodeBuilder<TNodeID, TActionID, TEntity, TContext> IsVisibleWhen(
            Expression<Func<TEntity, TContext, bool>> criteria)
        {
            this.Node.IsVisibleExpr = criteria;
            return this;
        }

        /// <summary>
        /// Adds an expressions that determine if an entity's field is visible by specified context
        /// </summary>
        public NodeBuilder<TNodeID, TActionID, TEntity, TContext> IsVisibleWhen(
            Expression<Func<TEntity, TContext, bool>> criteria,
            Expression<Func<TEntity, object>> forField)
        {
            this.Node.FieldIsVisibleExprs[ExpressionHelper.GetExpressionText(forField)] = criteria;
            return this;
        }

        /// <summary>
        /// Adds an expressions that determine if an entity is editable by specified context
        /// </summary>
        public NodeBuilder<TNodeID, TActionID, TEntity, TContext> IsEditableWhen(
            Expression<Func<TEntity, TContext, bool>> criteria)
        {
            this.Node.IsEditableExpr = criteria;
            return this;
        }

        /// <summary>
        /// Adds an expressions that determine if an entity's field is editable by specified context
        /// </summary>
        public NodeBuilder<TNodeID, TActionID, TEntity, TContext> IsEditableWhen(
            Expression<Func<TEntity, TContext, bool>> criteria,
            Expression<Func<TEntity, object>> forField)
        {
            this.Node.FieldIsEditableExprs[ExpressionHelper.GetExpressionText(forField)] = criteria;
            return this;
        }

        /// <summary>
        /// Adds a valua that determine if an entity is editable by specified context
        /// </summary>
        public NodeBuilder<TNodeID, TActionID, TEntity, TContext> IsEditable(
            bool value)
        {
            return IsEditableWhen((t, ctx) => value);
        }

        /// <summary>
        /// Defines that node edges are not exclusive
        /// </summary>
        public NodeBuilder<TNodeID, TActionID, TEntity, TContext> NotExclusiveActions()
        {
            this.Node.EdgesAreExclusive = false;
            return this;
        }

        /// <summary>
        /// Adds an edge
        /// </summary>
        /// <param name="config">A function used to configure edge</param>
        /// <returns></returns>
        public NodeBuilder<TNodeID, TActionID, TEntity, TContext> Permit(
            TActionID actionID,
            Action<EdgeBuilder<TNodeID, TActionID, TEntity, TContext>> config)
        {
            var edgeBuilder = new EdgeBuilder<TNodeID, TActionID, TEntity, TContext>(actionID);
            config(edgeBuilder);
            this.Node.Edges.Add(edgeBuilder.Edge);
            return this;
        }
    }
}