let DefaultPageNo = 1;
let DefaultPageSize = 10;
var tableState = {
    pageNo: DefaultPageNo,
    pageSize: DefaultPageSize,
    totalRecords: 0,
    currentTable: '',
    callback: null
};
function initTable(config) {
    tableState.pageNo = config.pageNo || DefaultPageNo;
    tableState.pageSize = config.pageSize || DefaultPageSize;
    tableState.totalRecords = config.totalRecords || 0;
    tableState.currentTable = config.tableName;
    tableState.callback = config.onPageChange;
    updateTableFooter();
}
function updateTableFooter() {
    let start = ((tableState.pageNo - 1) * tableState.pageSize) + 1;
    let end = Math.min(start + tableState.pageSize - 1, tableState.totalRecords);
    if (tableState.totalRecords === 0) {
        start = 0;
        end = 0;
    }
    const tableInfo = document.getElementById('tableInfo');
    if (tableInfo) {
        tableInfo.innerText = `Showing ${start} to ${end} of ${tableState.totalRecords} entries`;
    }
    const currentPage = document.getElementById('currentPage');
    if (currentPage) {
        currentPage.innerText = tableState.pageNo;
    }

    let maxPage = Math.ceil(tableState.totalRecords / tableState.pageSize);
    const prevBtn = document.getElementById('btnPrevPage');
    if (prevBtn) {
        if (tableState.pageNo <= 1) {
            prevBtn.disabled = true;
            prevBtn.classList.add('opacity-50', 'cursor-not-allowed');
        } else {
            prevBtn.disabled = false;
            prevBtn.classList.remove('opacity-50', 'cursor-not-allowed');
        }
    }
    const nextBtn = document.getElementById('btnNextPage');
    if (nextBtn) {
        if (tableState.pageNo >= maxPage || maxPage <= 1) {
            nextBtn.disabled = true;
            nextBtn.classList.add('opacity-50', 'cursor-not-allowed');
        } else {
            nextBtn.disabled = false;
            nextBtn.classList.remove('opacity-50', 'cursor-not-allowed');
        }
    }
}
function changePage(direction) {
    let maxPage = Math.ceil(tableState.totalRecords / tableState.pageSize);
    if (direction === -1 && tableState.pageNo > 1) {
        tableState.pageNo--;
    }
    else if (direction === 1 && tableState.pageNo < maxPage) {
        tableState.pageNo++;
    }
    else {
        return;
    }
    if (typeof tableState.callback === "function") {
        tableState.callback(tableState.pageNo, tableState.pageSize);
    }
}
function onPageSizeChange(size) {
    tableState.pageNo = DefaultPageNo;
    tableState.pageSize = parseInt(size);
    tableState.callback(tableState.pageNo, tableState.pageSize);
}
function onTableSearch(text) {
    tableState.pageNo = DefaultPageNo;
    tableState.search = text;
    tableState.callback(tableState.pageNo, tableState.pageSize);
}
function loadCommonTable(config) {
    var filter_list = '';
    if (config.filters && config.filters.length > 0) {
        filter_list = JSON.stringify(config.filters);
    }
    $.ajax({
        url: config.url || '/Common/GetCommonList',
        type: 'POST',
        data: {
            flag: config.flag,
            PageNo: config.pageNo,
            PageSize: config.pageSize,
            searchcriteria: tableState.search || '',
            filter_list: filter_list
        },
        success: function (res) {
            if (res.status !== 'success') {
                return;
            }
            var tableData = JSON.parse(res.data);
            if (typeof config.bind === 'function') {
                config.bind(tableData);
            }
            var totalRecords = tableData.length > 0
                ? tableData[0].overall_count
                : 0;

            initTable({
                tableName: config.tableName,
                pageNo: config.pageNo,
                pageSize: config.pageSize,
                totalRecords: totalRecords,
                onPageChange: function (p, s) {
                    config.onPageChange(p, s);
                }
            });
        }
    });
}
