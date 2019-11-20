using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using MockQueryable.Moq;
using System.Threading.Tasks;

namespace PostCore.Utils.Tests
{
    public class PaginatedListTest
    {
        public class Entity
        {
            public Entity(int value) => this.value = value;
            public int value = 0;
        }

        List<Entity> MakeList()
        {
            var l = new List<Entity>();
            for (int i = 0; i < 10; ++i)
            {
                l.Add(new Entity(i));
            }

            return l;
        }

        [Fact]
        public void CheckPaginationInfoHasProperties()
        {
            var totalPages = 5;
            var pageSize = 5;
            var piHasNoPrev = new PaginationInfo
            {
                CurrentPage = 1,
                PageSize = pageSize,
                TotalPages = totalPages,
            };

            Assert.False(piHasNoPrev.HasPreviousPage);
            Assert.True(piHasNoPrev.HasNextPage);

            var piHasNoNext = new PaginationInfo
            {
                CurrentPage = totalPages,
                PageSize = pageSize,
                TotalPages = totalPages,
            };
            Assert.True(piHasNoNext.HasPreviousPage);
            Assert.False(piHasNoNext.HasNextPage);

            var piHasBoth = new PaginationInfo
            {
                CurrentPage = 2,
                PageSize = pageSize,
                TotalPages = totalPages,
            };
            Assert.True(piHasBoth.HasPreviousPage);
            Assert.True(piHasBoth.HasNextPage);

            var piHasNone = new PaginationInfo
            {
                CurrentPage = 1,
                PageSize = pageSize,
                TotalPages = 1,
            };
            Assert.False(piHasNone.HasPreviousPage);
            Assert.False(piHasNone.HasNextPage);
        }

        [Fact]
        public async Task Create()
        {
            Assert.NotNull(MakeList().ToPaginatedList(2, 3));
            Assert.NotNull(await MakeList()
                .AsQueryable()
                .BuildMock()
                .Object
                .ToPaginatedListAsync(2, 3));
        }

        [Fact]
        public void CheckPaginationInfoValues()
        {
            var l = MakeList();
            const long pageSize = 3;
            const long page = 2;
            var totalPages = (long)Math.Ceiling(l.Count() / (double)pageSize);

            var pl = l.ToPaginatedList(page, pageSize);
            Assert.Equal(page, pl.PaginationInfo.CurrentPage);
            Assert.Equal(pageSize, pl.PaginationInfo.PageSize);
            Assert.Equal(totalPages, pl.PaginationInfo.TotalPages);
        }

        [Fact]
        public void CheckElements()
        {
            var l = MakeList();
            const long pageSize = 3;
            const long page = 2;

            var pl = l.ToPaginatedList(page, 3);
            Assert.Equal(pageSize, pl.Count());
            for (int i = 0; i < pageSize; ++i)
            {
                var expectedValue = pageSize * (page - 1) + i;
                Assert.Equal(expectedValue, pl[i].value);
            }
        }
    }
}
