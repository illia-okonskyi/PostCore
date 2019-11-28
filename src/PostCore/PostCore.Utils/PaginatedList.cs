using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace PostCore.Utils
{
    public class PaginationInfo
    {
        public long CurrentPage { get; set; }
        public long PageSize { get; set; }
        public long TotalPages { get; set; }
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }

    public class PaginatedList<T> : List<T>
    {
        public PaginatedList()
        {
            PaginationInfo = new PaginationInfo
            {
                PageSize = 1,
                CurrentPage = 1,
                TotalPages = 1
            };
        }

        public PaginatedList(long pageSize)
        {
            PaginationInfo = new PaginationInfo
            {
                PageSize = Math.Max(1, pageSize),
                CurrentPage = 1,
                TotalPages = 1
            };
        }

        public PaginatedList(IEnumerable<T> source, long count, long currentPage, long pageSize)
        {
            PaginationInfo = new PaginationInfo
            {
                PageSize = Math.Max(1, pageSize),
                CurrentPage = Math.Max(1, currentPage),
                TotalPages = Math.Max(1, (long)Math.Ceiling(count / (double)pageSize))
            };
            AddRange(source);
        }

        public static PaginatedList<T> Create(
            IEnumerable<T> source,
            long currentPage,
            long pageSize)
        {
            var count = source.Count();
            var items = source
                .Skip((int)((currentPage - 1) * pageSize))
                .Take((int)pageSize);
            return new PaginatedList<T>(items, count, currentPage, pageSize);
        }

        public static async Task<PaginatedList<T>> CreateAsync(
            IQueryable<T> source,
            long currentPage,
            long pageSize)
        {
            var count = await source.CountAsync();
            var items = await source
                .Skip((int)((currentPage - 1) * pageSize))
                .Take((int)pageSize)
                .ToListAsync();
            return new PaginatedList<T>(items, count, currentPage, pageSize);
        }

        public PaginationInfo PaginationInfo { get; private set; }
    }
}
