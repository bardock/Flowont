using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace Bardock.Flowont
{
    public static class ExpressionExtensions
    {
        private static ConcurrentDictionary<int, object> cache = new ConcurrentDictionary<int, object>();

        public static Func<TEntity, TContext, bool> CompileCached<TEntity, TContext>(
            this Expression<Func<TEntity, TContext, bool>> @this)
            where TEntity : class
        {
            return (Func<TEntity, TContext, bool>)cache.GetOrAdd(
                key: @this.GetHashCode(),
                valueFactory: _ => @this.Compile());
        }
    }
}