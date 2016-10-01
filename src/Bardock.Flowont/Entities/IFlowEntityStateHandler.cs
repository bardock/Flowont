using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Bardock.Flowont.Entities
{
    /// <summary>
    /// Handles entity state changes through the flow
    /// </summary>
    public interface IFlowEntityStateHandler<TEntity, TNodeID, TActionID, TContext>
        where TEntity : class, Entities.IFlowEntity
        where TNodeID : struct, IConvertible
        where TActionID : struct, IConvertible
    {
        Expression<Func<TEntity, bool>> GetIsInNodeExpression(TNodeID nodeID);

        IList<TNodeID> GetCurrentNodeIDs(TEntity e);

        void SetCurrentNodeIDs(TEntity e, IList<TNodeID> ids);

        void OnTransitions(TEntity e, IList<Entities.PathLog<TNodeID, TActionID>> logs, TContext ctx);
    }
}