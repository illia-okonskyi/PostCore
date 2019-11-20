using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace PostCore.Utils.Tests
{
    public class IQueryableExtenstionsTest
    {
        public class NestedProperty
        {
            public int SortProperty2 { get; set; }
        }

        public class Entity
        {
            public int Id { get; set; }
            public int SortProperty1 { get; set; }
            public NestedProperty NestedProperty { get; set; }
        }

        List<Entity> MakeList(int seed)
        {
            var r = new Random(seed);
            var l = new List<Entity>();
            for (int i = 0; i < 10; ++i)
            {
                l.Add(new Entity
                {
                    Id = i,
                    SortProperty1 = r.Next(),
                    NestedProperty = new NestedProperty
                    {
                        SortProperty2 = r.Next()
                    }
                });
            }

            return l;
        }

        [Fact]
        public void TestOrder()
        {
            var l = MakeList(1);
            var q = new List<Entity>(l).AsQueryable();

            Assert.Equal(l, q.Order(null, SortOrder.Ascending).ToList());
            Assert.Equal(l, q.Order("", SortOrder.Descending).ToList());
            Assert.Equal(l, q.Order("123", SortOrder.NoSort).ToList());

            Assert.Equal(
                l.OrderBy(e => e.SortProperty1).ToList(),
                q.Order("SortProperty1", SortOrder.Ascending).ToList());

            Assert.Equal(
                l.OrderByDescending(e => e.NestedProperty.SortProperty2).ToList(),
                q.Order("NestedProperty.SortProperty2", SortOrder.Descending).ToList());
        }
    }
}
