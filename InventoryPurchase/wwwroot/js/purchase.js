// Purchase form - add / remove rows and calculate amounts.
// This is only for showing amounts on screen.
// Actual amounts are calculated and saved by sp_Purchase_Save.

function toNumber(value) {
    var n = parseFloat(value);
    return isNaN(n) ? 0 : n;
}

function round2(n) {
    return Math.round(n * 100) / 100;
}

function getField(row, field) {
    return row.querySelector("[data-field='" + field + "']");
}

function addRow() {
    var tbody = document.querySelector("#lines tbody");
    var firstRow = tbody.querySelector("tr.line");
    var newRow = firstRow.cloneNode(true);

    newRow.querySelectorAll("input").forEach(function (input) {
        input.value = "";
    });
    getField(newRow, "DiscountPercent").value = "0";
    getField(newRow, "ProductId").value = "0";
    getField(newRow, "ExpiryDate").disabled = false;

    tbody.appendChild(newRow);
    calculate();
}

function removeRow(button) {
    var tbody = document.querySelector("#lines tbody");
    if (tbody.querySelectorAll("tr.line").length == 1) {
        alert("At least one row is required");
        return;
    }
    button.closest("tr").remove();
    calculate();
}

function productChanged(select) {
    var row = select.closest("tr");
    var option = select.options[select.selectedIndex];

    // fill cost price as default rate
    var rate = getField(row, "Rate");
    if (rate.value == "" && option.dataset.cost) {
        rate.value = toNumber(option.dataset.cost).toFixed(2);
    }

    setExpiryBox(row);
    calculate();
}

function setExpiryBox(row) {
    var select = getField(row, "ProductId");
    var option = select.options[select.selectedIndex];
    var expiry = getField(row, "ExpiryDate");

    if (option.dataset.expiry == "0") {
        expiry.value = "";
        expiry.disabled = true;
    } else {
        expiry.disabled = false;
    }
}

function calculate() {
    var totalGross = 0, totalDiscount = 0, totalTax = 0, totalNet = 0;

    document.querySelectorAll("#lines tbody tr.line").forEach(function (row) {
        var select = getField(row, "ProductId");
        var option = select.options[select.selectedIndex];

        var qty = toNumber(getField(row, "Quantity").value);
        var rate = toNumber(getField(row, "Rate").value);
        var discPercent = toNumber(getField(row, "DiscountPercent").value);

        var gross = round2(qty * rate);
        var discount = round2(gross * discPercent / 100);
        var taxable = gross - discount;

        var taxRate = toNumber(option.dataset.taxrate);
        var addRate = toNumber(option.dataset.addrate);

        var tax1 = round2(taxable * taxRate / 100);
        var tax2 = 0;
        if (option.dataset.taxtype == "Compound") {
            tax2 = round2((taxable + tax1) * addRate / 100);
        }
        var tax = tax1 + tax2;
        var net = taxable + tax;

        row.querySelector(".gross").innerText = gross.toFixed(2);
        row.querySelector(".tax").innerText = tax.toFixed(2);
        row.querySelector(".net").innerText = net.toFixed(2);

        totalGross += gross;
        totalDiscount += discount;
        totalTax += tax;
        totalNet += net;
    });

    document.getElementById("totalGross").innerText = totalGross.toFixed(2);
    document.getElementById("totalDiscount").innerText = totalDiscount.toFixed(2);
    document.getElementById("totalTax").innerText = totalTax.toFixed(2);
    document.getElementById("totalNet").innerText = totalNet.toFixed(2);
}

// give names like Details[0].ProductId so that MVC can bind the list
function setNames() {
    var rows = document.querySelectorAll("#lines tbody tr.line");
    for (var i = 0; i < rows.length; i++) {
        rows[i].querySelectorAll("[data-field]").forEach(function (input) {
            input.name = "Details[" + i + "]." + input.dataset.field;
        });
    }
}

document.getElementById("purchaseForm").addEventListener("submit", function () {
    setNames();
});

document.querySelectorAll("#lines tbody tr.line").forEach(function (row) {
    setExpiryBox(row);
});
calculate();
