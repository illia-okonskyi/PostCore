using System.Collections.Generic;
using System.Linq;

namespace PostCore.Utils
{
    public static class IEnumerableExtenstions
    {
        public static IEnumerable<T> Order<T>(
            this IEnumerable<T> query,
            string sortKey,
            SortOrder sortOrder) where T : class
        {
            return query.AsQueryable().Order(sortKey, sortOrder).AsEnumerable();
        }

        public static PaginatedList<T> ToPaginatedList<T>(
            this IEnumerable<T> source,
            long currentPage,
            long pageSize)
        {
            return PaginatedList<T>.Create(source, currentPage, pageSize);
        }
    }
}
