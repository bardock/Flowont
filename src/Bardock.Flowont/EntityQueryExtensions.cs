using Bardock.Utils.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Bardock.Flowont
{
    public static class EntityQueryExtensions
    {
        /// <summary>
        /// Filters a sequence of values based on a predicate.
        /// If checkFalsePredicate is true and predicate is a constant expression with false as value,
        /// returns an empty list in order to avoid overhead of query execution.
        /// </summary>
        /// <param name="predicateFactory">Specifies a function that returns the predicate. It provides lazy evaluation.</param>
        private static IQueryable<TSource> Where<TSource>(
            this IQueryable<TSource> source,
            Func<Expression<Func<TSource, bool>>> predicateFactory,
            bool checkFalsePredicate,
            bool when = true)
        {
            if (!when)
                return source;

            var predicate = predicateFactory();

            if (checkFalsePredicate && predicate.IsConstant(false))
                return new List<TSource>().AsQueryable();

            return source.Where(predicate);
        }

        /// <summary>
        /// Filters entities that are in specified node
        /// </summary>
        public static IQueryable<TEntity> WhereIsInNode<TFlowID, TNodeID, TActionID, TEntity, TContext>(
            this IQueryable<TEntity> source,
            TNodeID nodeID,
            FlowDefinitions<TFlowID, TNodeID, TActionID, TEntity, TContext> flowDefinitions,
            bool when = true)
            where TEntity : class, Entities.IMultiFlowEntity<TFlowID>
            where TNodeID : struct, IConvertible
            where TActionID : struct, IConvertible
        {
            return source.Where(
                () => flowDefinitions.GetIsInNodeExpression(nodeID),
                checkFalsePredicate: true,
                when: when);
        }

        /// <summary>
        /// Filters entities that are visible by specified context
        /// </summary>
        public static IQueryable<TEntity> WhereIsVisible<TFlowID, TNodeID, TActionID, TEntity, TContext>(
            this IQueryable<TEntity> source,
            TContext ctx,
            FlowDefinitions<TFlowID, TNodeID, TActionID, TEntity, TContext> flowDefinitions,
            bool when = true)
            where TEntity : class, Entities.IMultiFlowEntity<TFlowID>
            where TNodeID : struct, IConvertible
            where TActionID : struct, IConvertible
        {
            return source.Where(
                () => flowDefinitions.GetIsVisibleExpression(ctx),
                checkFalsePredicate: true,
                when: when);
        }

        /// <summary>
        /// Filters entities where the action is allowed for specified context
        /// </summary>
        public static IQueryable<TEntity> WhereActionIsAllowed<TFlowID, TNodeID, TActionID, TEntity, TContext>(
            this IQueryable<TEntity> source,
            TActionID actionID,
            TContext ctx,
            FlowDefinitions<TFlowID, TNodeID, TActionID, TEntity, TContext> flowDefinitions,
            bool when = true)
            where TEntity : class, Entities.IMultiFlowEntity<TFlowID>
            where TNodeID : struct, IConvertible
            where TActionID : struct, IConvertible
        {
            return source.Where(
                () => flowDefinitions.GetActionIsAllowedExpression(actionID, ctx),
                checkFalsePredicate: true,
                when: when);
        }

        /// <summary>
        /// Filters entities where the action is allowed.
        /// It does not evaluates any rule that depends on a context.
        /// </summary>
        public static IQueryable<TEntity> WhereActionIsAllowed<TFlowID, TNodeID, TActionID, TEntity, TContext>(
            this IQueryable<TEntity> source,
            TActionID actionID,
            FlowDefinitions<TFlowID, TNodeID, TActionID, TEntity, TContext> flowDefinitions,
            bool when = true)
            where TEntity : class, Entities.IMultiFlowEntity<TFlowID>
            where TNodeID : struct, IConvertible
            where TActionID : struct, IConvertible
        {
            return source.Where(
                () => flowDefinitions.GetActionIsAllowedExpression(actionID),
                checkFalsePredicate: true,
                when: when);
        }

        /// <summary>
        /// Filters entities where the action is allowed.
        /// It does not evaluates any rule that depends on a context.
        /// </summary>
        public static IQueryable<TEntity> WhereIsAnyActionAllowed<TFlowID, TNodeID, TActionID, TEntity, TContext>(
            this IQueryable<TEntity> source,
            TActionID[] actionIDs,
            TContext ctx,
            FlowDefinitions<TFlowID, TNodeID, TActionID, TEntity, TContext> flowDefinitions,
            bool when = true)
            where TEntity : class, Entities.IMultiFlowEntity<TFlowID>
            where TNodeID : struct, IConvertible
            where TActionID : struct, IConvertible
        {
            return source.Where(
                () => flowDefinitions.GetActionIsAllowedExpression(actionIDs, ctx),
                checkFalsePredicate: true,
                when: when);
        }

        /// <summary>
        /// Filters entities where the action is allowed.
        /// It does not evaluates any rule that depends on a context.
        /// </summary>
        public static IQueryable<TEntity> WhereIsAnyActionAllowed<TFlowID, TNodeID, TActionID, TEntity, TContext>(
            this IQueryable<TEntity> source,
            TActionID[] actionIDs,
            FlowDefinitions<TFlowID, TNodeID, TActionID, TEntity, TContext> flowDefinitions,
            bool when = true)
            where TEntity : class, Entities.IMultiFlowEntity<TFlowID>
            where TNodeID : struct, IConvertible
            where TActionID : struct, IConvertible
        {
            return source.Where(
                () => flowDefinitions.GetActionIsAllowedExpression(actionIDs),
                checkFalsePredicate: true,
                when: when);
        }

        /// <summary>
        /// Filters entities that are visible by specified context
        /// </summary>
        public static IQueryable<TEntity> WhereIsAnyActionAllowed<TFlowID, TNodeID, TActionID, TEntity, TContext>(
            this IQueryable<TEntity> source,
            TContext ctx,
            FlowDefinitions<TFlowID, TNodeID, TActionID, TEntity, TContext> flowDefinitions,
            bool when = true)
            where TEntity : class, Entities.IMultiFlowEntity<TFlowID>
            where TNodeID : struct, IConvertible
            where TActionID : struct, IConvertible
        {
            return source.Where(
                () => flowDefinitions.GetIsAnyActionAllowedExpression(ctx),
                checkFalsePredicate: true,
                when: when);
        }
    }
}