using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace PostCore.Utils
{
    [TypeConverter(typeof(ListOptionsConverter))]
    public class ListOptions
    {
        // Filters[FilterKey] = filterValue
        public Dictionary<string, string> Filters { get; set; } = new Dictionary<string, string>();

        public string SortKey { get; set; }
        public SortOrder SortOrder { get; set; } = SortOrder.NoSort;

        public long Page { get; set; } = 1;

        public override string ToString()
        {
            var converter = TypeDescriptor.GetConverter(this);
            return converter.ConvertToString(this);
        }
    }

    public class ListOptionsConverter : TypeConverter
    {
        const char PartsSplitter = '&';
        const char FilterKeyValuePairsSplitter = ';';
        const char FilterKeyValueSplitter = ':';
        const string FiltersPrefix = "filters=";
        const string SortKeyPrefix = "sortKey=";
        const string SortOrderPrefix = "sortOrder=";
        const string PagePrefix = "page=";

        public override bool CanConvertFrom(
            ITypeDescriptorContext context,
            Type sourceType)
        {
            if (sourceType == typeof(string) || sourceType == typeof(object))
            {
                return true;
            }
            return base.CanConvertFrom(context, sourceType);
        }

        // Overrides the ConvertFrom method of TypeConverter.
        public override object ConvertFrom(
            ITypeDescriptorContext context,
            CultureInfo culture,
            object value)
        {
            if (value == null)
            {
                return null;
            }

            if (!(value is string))
            {
                return base.ConvertFrom(context, culture, value);
            }

            // 1) Split parts
            var parts = ((string)value).Split(PartsSplitter);
            if (parts.Length < 4)
            {
                return null;
            }

            var filtersString = parts[0];
            var sortKeyString = parts[1];
            var sortOrderString = parts[2];
            var pageString = parts[3];

            // 2) Check prefixes
            if (!(filtersString.StartsWith(FiltersPrefix) &&
                sortKeyString.StartsWith(SortKeyPrefix) &&
                sortOrderString.StartsWith(SortOrderPrefix) &&
                pageString.StartsWith(PagePrefix)))
            {
                return null;
            }

            // 3) Remove prefixes
            filtersString = filtersString.Substring(FiltersPrefix.Length);
            sortKeyString = sortKeyString.Substring(SortKeyPrefix.Length);
            sortOrderString = sortOrderString.Substring(SortOrderPrefix.Length);
            pageString = pageString.Substring(PagePrefix.Length);

            var options = new ListOptions();

            // 4) Parse and set filters
            if (!string.IsNullOrEmpty(filtersString))
            {
                var filterKeyValues = filtersString.Split(FilterKeyValuePairsSplitter);
                foreach (var keyValueString in filterKeyValues)
                {
                    var keyValueParts = keyValueString.Split(FilterKeyValueSplitter);
                    if (keyValueParts.Length != 2)
                    {
                        return null;
                    }
                    options.Filters.Add(keyValueParts[0], keyValueParts[1]);
                }
            }

            // 5) Set sort key
            options.SortKey = sortKeyString;

            // 6) Parse and set sort order
            if (!string.IsNullOrEmpty(sortOrderString))
            {
                if (!Enum.TryParse(sortOrderString, true, out SortOrder sortOrder))
                {
                    sortOrder = SortOrder.NoSort;
                }
                options.SortOrder = sortOrder;
            }

            // 6) Parse and set page
            if (!string.IsNullOrEmpty(pageString))
            {
                if (!long.TryParse(pageString, out long page))
                {
                    page = 1;
                }
                options.Page = page;
            }

            return options;
        }

        public override bool CanConvertTo(
            ITypeDescriptorContext context,
            Type destinationType)
        {
            if (destinationType == typeof(ListOptions))
            {
                return true;
            }
            return base.CanConvertTo(context, destinationType);
        }

        public override object ConvertTo(
            ITypeDescriptorContext context,
            CultureInfo culture,
            object value,
            Type destinationType)
        {
            if (destinationType != typeof(string))
            {
                return base.ConvertTo(context, culture, value, destinationType);
            }

            if (value == null)
            {
                return null;
            }

            var options = (ListOptions)value;

            // 1) Build strings
            var filtersString = FiltersPrefix + string.Join(
                FilterKeyValuePairsSplitter,
                options.Filters.Select(kvp => kvp.Key + FilterKeyValueSplitter + kvp.Value)
                );
            var sortKeyString = SortKeyPrefix + options.SortKey;
            var sortOrderString = SortOrderPrefix + options.SortOrder.ToString();
            var pageString = PagePrefix + options.Page.ToString();

            // 2) Build resulting string
            return string.Join(
                PartsSplitter,
                filtersString,
                sortKeyString,
                sortOrderString,
                pageString);
        }
    }

}
