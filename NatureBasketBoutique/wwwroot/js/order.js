var dataTable;

$(document).ready(function () {
    var url = window.location.search;
    if (url.includes("inprocess")) {
        loadDataTable("inprocess");
    }
    else {
        if (url.includes("completed")) {
            loadDataTable("completed");
        }
        else {
            if (url.includes("pending")) {
                loadDataTable("pending");
            }
            else {
                if (url.includes("approved")) {
                    loadDataTable("approved");
                }
                else {
                    loadDataTable("all");
                }
            }
        }
    }
});

function loadDataTable(status) {
    dataTable = $('#tblData').DataTable({
        "ajax": {
            "url": "/Admin/Order/GetAll?status=" + status
        },
        "columns": [
            // 1. DISPLAY ID (Shows A-101 instead of 101)
            { "data": "displayId", "width": "5%" },

            { "data": "name", "width": "20%" },
            { "data": "phoneNumber", "width": "15%" },
            { "data": "applicationUser.email", "width": "20%" },
            {
                "data": "orderStatus",
                "width": "10%",
                "render": function (data) {
                    if (data == "Shipped") return `<span class="badge bg-success">${data}</span>`;
                    if (data == "Cancelled") return `<span class="badge bg-danger">${data}</span>`;
                    if (data == "InProcess") return `<span class="badge bg-info text-dark">${data}</span>`;
                    return `<span class="badge bg-secondary">${data}</span>`;
                }
            },
            { "data": "orderTotal", "width": "10%" },
            {
                "data": "id", // We keep the REAL ID here for the link
                "render": function (data) {
                    return `
                        <div class="w-75 btn-group" role="group">
                        <a href="/Admin/Order/Details?orderId=${data}"
                        class="btn btn-primary mx-2"> <i class="bi bi-pencil-square"></i></a>
                        </div>
                    `
                },
                "width": "10%"
            }
        ]
    });
}