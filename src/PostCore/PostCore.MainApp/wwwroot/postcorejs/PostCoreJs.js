"use strict";

var PostCoreJs = {};
(function (module, $) {
    class ListOptionsContext {
        static SortOrderNoSort = "NoSort";
        static SortOrderAscending = "Ascending";
        static SortOrderDescending = "Descending";

        static _partsSplitter = '&';
        static _filterKeyValuePairsSplitter = ';';
        static _filterKeyValueSplitter = ':';
        static _filtersPrefix = "filters=";
        static _sortKeyPrefix = "sortKey=";
        static _sortOrderPrefix = "sortOrder=";
        static _pagePrefix = "page=";

        _context = {
            filters: {},
            sortKey: "",
            sortOrder: this.SortOrderNoSort,
            page: 1,
            formId: "",
            optionsInputId: "",
            filterKeyToInputId: {},
            resetPageOnSubmit: true
        };

        _makeParseError(message) {
            return new Error("Parse error: " + message);
        }

        constructor(currentListOptions) {
            this._parseCurrentListOptions(currentListOptions);
        }

        _parseCurrentListOptions(currentListOptions) {
            if (!currentListOptions) {
                return;
            }

            currentListOptions = $.parseHTML(currentListOptions)[0].textContent;

            // 1) Split parts
            var parts = currentListOptions.split(ListOptionsContext._partsSplitter);
            if (parts.length < 4) {
                throw this._makeParseError("Parts count < 4");
            }

            var filtersString = parts[0];
            var sortKeyString = parts[1];
            var sortOrderString = parts[2];
            var pageString = parts[3];

            // 2) Check prefixes
            if (!(filtersString.startsWith(ListOptionsContext._filtersPrefix) &&
                sortKeyString.startsWith(ListOptionsContext._sortKeyPrefix) &&
                sortOrderString.startsWith(ListOptionsContext._sortOrderPrefix) &&
                pageString.startsWith(ListOptionsContext._pagePrefix))) {
                throw this._makeParseError("Parts prefixes are missed");
            }

            // 3) Remove prefixes
            filtersString = filtersString.substring(ListOptionsContext._filtersPrefix.length);
            sortKeyString = sortKeyString.substring(ListOptionsContext._sortKeyPrefix.length);
            sortOrderString = sortOrderString.substring(ListOptionsContext._sortOrderPrefix.length);
            pageString = pageString.substring(ListOptionsContext._pagePrefix.length);

            // 4) Parse and set filters
            if (filtersString) {
                var filterKeyValues = filtersString.split(ListOptionsContext._filterKeyValuePairsSplitter);
                filterKeyValues.forEach(function (keyValueString) {
                    var keyValueParts = keyValueString.split(ListOptionsContext._filterKeyValueSplitter);
                    if (keyValueParts.length != 2) {
                        throw this._makeParseError("Wrong filter string");
                    }
                    this._context.filters[keyValueParts[0]] = keyValueParts[1];
                }.bind(this));
            }


            // 5) Set sort key and order and page
            this._context.sortKey = sortKeyString;
            this._context.sortOrder = sortOrderString;
            if (pageString) {
                this._context.page = pageString;
            }
        }

        filter(key) {
            return this._context.filters[key];
        }
        setFilter(key, value) {
            this._context.filters[key] = value;
        }

        get sortKey() {
            return this._context.sortKey;
        }
        set sortKey(value) {
            this._context.sortKey = value;
        }

        get sortOrder() {
            return this._context.sortOrder;
        }
        set sortOrder(value) {
            this._context.sortOrder = value;
        }

        get page() {
            return this._context.page;
        }
        set page(value) {
            this._context.page = value;
        }

        get formId() {
            return this._context.formId;
        }
        set formId(value) {
            this._context.formId = value;
        }

        get optionsInputId() {
            return this._context.optionsInputId;
        }
        set optionsInputId(value) {
            this._context.optionsInputId = value;
        }

        get resetPageOnSubmit() {
            return this._context.resetPageOnSubmit;
        }
        set resetPageOnSubmit(value) {
            this._context.resetPageOnSubmit = value;
        }

        addFilterKeyInputPair(filterKey, inputId) {
            if (!this._context.filters[filterKey]) {
                this._context.filters[filterKey] = "";
            }
            this._context.filterKeyToInputId[filterKey] = inputId;
        }

        setupSubmitButton(buttonId) {
            var buttonSelector = "#" + buttonId;
            $(buttonSelector).on("click", function () {
                this.submitForm();
            }.bind(this));

            Object.getOwnPropertyNames(this._context.filterKeyToInputId)
                .forEach(function (filterKey) {
                    var inputSelector = "#" + this._context.filterKeyToInputId[filterKey];
                    $(inputSelector).keypress(function (e) {
                        if (e.which == 13) {
                            event.preventDefault();
                            $(buttonSelector).click();
                        }
                    });
                }.bind(this));
        }

        fillForm() {
            Object.getOwnPropertyNames(this._context.filters)
                .forEach(function (filterKey) {
                    var inputSelector = "#" + this._context.filterKeyToInputId[filterKey];
                    $(inputSelector).val(this.filter(filterKey));
                }.bind(this));
        }

        submitForm() {
            if (this._context.resetPageOnSubmit) {
                this._context.page = 1;
            }

            Object.getOwnPropertyNames(this._context.filters)
                .forEach(function (filterKey) {
                    var inputSelector = "#" + this._context.filterKeyToInputId[filterKey];
                    this.setFilter(filterKey, $(inputSelector).val());
                }.bind(this));

            var optionsSelector = "#" + this._context.formId + " #" + this._context.optionsInputId;
            var formSelector = "#" + this._context.formId;
            $(optionsSelector).val(this._buildOptions());
            $(formSelector).submit();
        }

        _buildOptions() {
            var filters = Object.getOwnPropertyNames(this._context.filters)
                .map(function (filterKey) {
                    return filterKey +
                        ListOptionsContext._filterKeyValueSplitter +
                        this._context.filters[filterKey];
                }.bind(this));

            var parts = [];
            parts.push(ListOptionsContext._filtersPrefix +
                filters.join(ListOptionsContext._filterKeyValuePairsSplitter)
            );
            parts.push(ListOptionsContext._sortKeyPrefix + this._context.sortKey);
            parts.push(ListOptionsContext._sortOrderPrefix + this._context.sortOrder);
            parts.push(ListOptionsContext._pagePrefix + this._context.page);

            return parts.join(ListOptionsContext._partsSplitter);
        }
    }

    // revealing module pattern: define exports
    var exports = {
        ListOptionsContext: ListOptionsContext,
        // etc.
    };

    // merge modules
    $.extend(module, exports);
    return module;

}(PostCoreJs, jQuery));


