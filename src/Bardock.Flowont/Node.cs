using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Bardock.Flowont
{
    /// <summary>
    /// Defines a flow's node
    /// </summary>
    public class Node<TNodeID, TActionID, TEntity, TContext>
        where TNodeID : struct, IConvertible
        where TActionID : struct, IConvertible
        where TEntity : class, Entities.IFlowEntity
    {
        public TNodeID ID { get; set; }

        /// <summary>
        /// Edges that connect with other nodes
        /// </summary>
        public IList<Edge<TNodeID, TActionID, TEntity, TContext>> Edges { get; set; }

        /// <summary>
        /// Determines if edges are exclusive (default) or not. If they are not exclusive, a transition does not replace source node if there are actions pending to be evaluated.
        /// </summary>
        public bool EdgesAreExclusive { get; set; }

        /// <summary>
        /// A list of functions to be executed when the entity enters in the node
        /// </summary>
        public IList<Action<TEntity, IList<Entities.PathLog<TNodeID, TActionID>>, TContext>> OnEntry { get; set; }

        /// <summary>
        /// A list of functions to be executed when the entity goes out of the node
        /// </summary>
        public IList<Action<TEntity, IList<Entities.PathLog<TNodeID, TActionID>>, TContext>> OnExit { get; set; }

        /// <summary>
        /// Expressions that determine if an entity is visible by specified context
        /// </summary>
        public Expression<Func<TEntity, TContext, bool>> IsVisibleExpr { get; set; }

        /// <summary>
        /// Expressions that determine if an entity's field is visible by specified context
        /// </summary>
        public IDictionary<string, Expression<Func<TEntity, TContext, bool>>> FieldIsVisibleExprs { get; set; }

        /// <summary>
        /// Expressions that determine if an entity is editable by specified context
        /// </summary>
        public Expression<Func<TEntity, TContext, bool>> IsEditableExpr { get; set; }

        /// <summary>
        /// Expressions that determine if an entity's field is editable by specified context
        /// </summary>
        public IDictionary<string, Expression<Func<TEntity, TContext, bool>>> FieldIsEditableExprs { get; set; }

        public Node()
        {
            this.Edges = new List<Edge<TNodeID, TActionID, TEntity, TContext>>();
            this.EdgesAreExclusive = true;
            this.OnEntry = new List<Action<TEntity, IList<Entities.PathLog<TNodeID, TActionID>>, TContext>>();
            this.OnExit = new List<Action<TEntity, IList<Entities.PathLog<TNodeID, TActionID>>, TContext>>();
            this.IsVisibleExpr = (e, ctx) => true;
            this.FieldIsVisibleExprs = new Dictionary<string, Expression<Func<TEntity, TContext, bool>>>();
            this.IsEditableExpr = (e, ctx) => true;
            this.FieldIsEditableExprs = new Dictionary<string, Expression<Func<TEntity, TContext, bool>>>();
        }
    }
}