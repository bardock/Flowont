using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Bardock.Flowont
{
    /// <summary>
    /// Defines a flow edge that connects two nodes by an action
    /// </summary>
    public class Edge<TNodeID, TActionID, TEntity, TContext>
        where TEntity : class, Entities.IFlowEntity
    {
        /// <summary>
        /// Action
        /// </summary>
        public TActionID ActionID { get; set; }

        /// <summary>
        /// Destination node
        /// </summary>
        public TNodeID NodeToID { get; set; }

        /// <summary>
        /// Determines if this edge belongs to happy path
        /// </summary>
        public bool IsHappyPath { get; set; }

        /// <summary>
        /// Rules that entity must satisfy in order to evaluate action
        /// </summary>
        public IList<Expression<Func<TEntity, TContext, bool>>> Rules { get; set; }

        /// <summary>
        /// Functions to be executed before transitioning
        /// </summary>
        public IList<Action<TEntity, TContext>> PreProcesses { get; set; }

        public Edge()
        {
            this.Rules = new List<Expression<Func<TEntity, TContext, bool>>>();
            this.PreProcesses = new List<Action<TEntity, TContext>>();
        }
    }
}