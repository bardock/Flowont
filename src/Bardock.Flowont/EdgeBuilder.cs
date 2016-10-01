using System;
using System.Linq.Expressions;

namespace Bardock.Flowont
{
    /// <summary>
    /// Build an edge declaratively
    /// </summary>
    public class EdgeBuilder<TNodeID, TActionID, TEntity, TContext>
        where TNodeID : struct, IConvertible
        where TActionID : struct, IConvertible
        where TEntity : class, Entities.IFlowEntity
    {
        public Edge<TNodeID, TActionID, TEntity, TContext> Edge { get; protected set; }

        public EdgeBuilder(TActionID actionID)
        {
            this.Edge = new Edge<TNodeID, TActionID, TEntity, TContext>()
            {
                ActionID = actionID
            };
        }

        /// <summary>
        /// Specifies that this edge belongs to happy path
        /// </summary>
        public EdgeBuilder<TNodeID, TActionID, TEntity, TContext> IsHappyPath()
        {
            this.Edge.IsHappyPath = true;
            return this;
        }

        /// <summary>
        /// Adds a rule for current action
        /// </summary>
        public EdgeBuilder<TNodeID, TActionID, TEntity, TContext> When(
            Expression<Func<TEntity, TContext, bool>> rule)
        {
            this.Edge.Rules.Add(rule);
            return this;
        }

        /// <summary>
        /// Adds a function to be executed before transitioning
        /// </summary>
        public EdgeBuilder<TNodeID, TActionID, TEntity, TContext> Then(
            Action<TEntity, TContext> preProcess)
        {
            this.Edge.PreProcesses.Add(preProcess);
            return this;
        }

        /// <summary>
        /// Defines the node to generate
        /// </summary>
        public EdgeBuilder<TNodeID, TActionID, TEntity, TContext> TransitionTo(
            TNodeID nodeID)
        {
            this.Edge.NodeToID = nodeID;
            return this;
        }
    }
}