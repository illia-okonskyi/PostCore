using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Xunit;

namespace PostCore.Utils.Tests
{
    public class ListOptionsTest
    {
        [Fact]
        public void TestTypeConverterFromString()
        {
            var converter = TypeDescriptor.GetConverter(typeof(ListOptions));

            // No object
            Assert.Null(converter.ConvertFrom(null));
            Assert.Null(converter.ConvertFrom(""));

            // Random string
            Assert.Null(converter.ConvertFrom("snghinrighnr"));

            // Wrong parts count
            Assert.Null(converter.ConvertFrom("p1&&p2&&p3"));

            // No parts prefixes
            Assert.Null(converter.ConvertFrom("1&&sortKey=1&&sortOrder=1&&page=1"));
            Assert.Null(converter.ConvertFrom("filters1&&1&&sortOrder=1&&page=1"));
            Assert.Null(converter.ConvertFrom("filters=1&&sortKey=1&&1&page=1"));
            Assert.Null(converter.ConvertFrom("filters=1&&sortKey=1&&sortOrder=1&&1"));

            // Wrong filters format
            Assert.Null(converter.ConvertFrom("filters=1&&sortKey=1&&sortOrder=1&&page=1"));
            Assert.Null(converter.ConvertFrom("filters=1;;&&sortKey=1&&sortOrder=1&&page=1"));

            // Defaults
            var lo = (ListOptions)converter.ConvertFrom("filters=&&sortKey=&&sortOrder=&&page=");
            Assert.NotNull(lo);
            Assert.Empty(lo.Filters);
            Assert.Equal("", lo.SortKey);
            Assert.Equal(SortOrder.NoSort, lo.SortOrder);
            Assert.Equal(1, lo.Page);

            // Fully ok
            lo = (ListOptions)converter.ConvertFrom("filters=key1::value1;;key2::value2;;key3::&&sortKey=sortKey&&sortOrder=Ascending&&page=3");
            Assert.NotNull(lo);
            Assert.Equal(new Dictionary<string, string>
            {
                { "key1", "value1" },
                { "key2", "value2" },
                { "key3", "" }
            },lo.Filters);
            Assert.Equal("sortKey", lo.SortKey);
            Assert.Equal(SortOrder.Ascending, lo.SortOrder);
            Assert.Equal(3, lo.Page);
        }

        [Fact]
        public void TestTypeConverterToString()
        {
            var converter = TypeDescriptor.GetConverter(typeof(ListOptions));

            // No object
            Assert.Null(converter.ConvertToString(null));


            // Defaults
            var lo = new ListOptions();
            Assert.Equal(
                "filters=&&sortKey=&&sortOrder=NoSort&&page=1",
                converter.ConvertToString(lo));

            // Filled
            lo.Filters = new Dictionary<string, string>
            {
                { "key1", "value1" },
                { "key2", "value2" },
                { "key3", null },
                { "key4", "" },
            };
            lo.SortKey = "sortKey";
            lo.SortOrder = SortOrder.Ascending;
            lo.Page = 3;
            Assert.Equal(
                "filters=key1::value1;;key2::value2;;key3::;;key4::&&sortKey=sortKey&&sortOrder=Ascending&&page=3",
                converter.ConvertToString(lo));
        }
    }
}
