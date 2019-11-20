using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace PostCore.Utils
{
    public static class IQueryableExtenstions
    {
        public static IQueryable<T> Order<T>(
            this IQueryable<T> query,
            string sortKey,
            SortOrder sortOrder) where T : class
        {
            if (string.IsNullOrEmpty(sortKey) || sortOrder == SortOrder.NoSort)
            {
                return query;
            }

            // Declare T x
            var parameter = Expression.Parameter(typeof(T), "x");
            // Access x.fullPropertyPath: e.g. x.outerProp.innerProp
            var source = sortKey.Split('.').Aggregate((Expression)parameter, Expression.Property);
            // Build lambda expression: Type(x.fullPropertyPath)(T) x => x.fullPropertyPath
            var lambda = Expression.Lambda(typeof(Func<,>).MakeGenericType(typeof(T), source.Type), source, parameter);
            var orderMethodName = sortOrder == SortOrder.Ascending ? "OrderBy" : "OrderByDescending";
            // Select order method: Order<T, Type(x.fullPropertyPath)>
            var orderMethod = typeof(Queryable).GetMethods().Single(m => {
                return m.Name == orderMethodName &&
                    m.IsGenericMethodDefinition &&
                    m.GetGenericArguments().Length == 2 &&
                    m.GetParameters().Length == 2;
            }).MakeGenericMethod(typeof(T), source.Type);

            // Invoke order extension method (no object is specified and target object is passed
            // as argument instead)
            return orderMethod.Invoke(null, new object[] { query, lambda }) as IQueryable<T>;
        }

        public static async Task<PaginatedList<T>> ToPaginatedListAsync<T>(
            this IQueryable<T> source,
            long currentPage,
            long pageSize)
        {
            return await PaginatedList<T>.CreateAsync(source, currentPage, pageSize);
        }
    }
}
