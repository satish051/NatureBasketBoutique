var dataTable;

$(document).ready(function () {
    console.log("Order.js is loading..."); // Debug message
    loadDataTable();
});

function loadDataTable() {
    dataTable = $('#tblData').DataTable({
        "ajax": {
            "url": '/Admin/Order/GetAll',
            "dataSrc": "data"
        },
        "columns": [
            { "data": 'id', "width": "5%" },
            { "data": 'name', "width": "20%" },
            { "data": 'phoneNumber', "width": "15%" }, // Matches your JSON
            {
                "data": 'applicationUser',
                "render": function (data) {
                    // Safety Check: If user is deleted/null, show "N/A"
                    return data ? data.email : "N/A";
                },
                "width": "20%"
            },
            { "data": 'orderStatus', "width": "15%" },
            { "data": 'orderTotal', "width": "10%" },
            {
                "data": 'id',
                "render": function (data) {
                    return `
                        <div class="w-75 btn-group" role="group">
                            <a href="/Admin/Order/Details?orderId=${data}" class="bg-blue-500 hover:bg-blue-700 text-white font-bold py-1 px-3 rounded">
                                <i class="bi bi-pencil-square"></i> Details
                            </a>
                        </div>
                    `
                },
                "width": "15%"
            }
        ]
    });
}