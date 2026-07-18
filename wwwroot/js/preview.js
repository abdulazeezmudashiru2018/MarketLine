document.addEventListener('DOMContentLoaded', function () {
    var STORAGE_KEY = 'marketline_pending_sale';
    var root = document.getElementById('receiptRoot');
    var msgEl = document.getElementById('previewMsg');
    var saveBtn = document.getElementById('btnSaveReceipt');
    var printBtn = document.getElementById('btnPrint');
    var backBtn = document.getElementById('btnBack');

    function currency(n) {
        return '₦' + new Intl.NumberFormat().format(Math.round(n));
    }
    function escapeHtml(str) {
        var d = document.createElement('div');
        d.textContent = str == null ? '' : String(str);
        return d.innerHTML;
    }
    function getAntiForgeryToken() {
        var el = document.querySelector('input[name="__RequestVerificationToken"]');
        return el ? el.value : '';
    }

    var raw = sessionStorage.getItem(STORAGE_KEY);
    if (!raw) {
        root.innerHTML = '<p class="receipt-loading">No pending sale found. <a href="/Sales/Create">Go back to Add Sales</a>.</p>';
        saveBtn.disabled = true;
        printBtn.disabled = true;
        return;
    }

    var data;
    try {
        data = JSON.parse(raw);
    } catch (err) {
        root.innerHTML = '<p class="receipt-loading">Something went wrong reading this sale. <a href="/Sales/Create">Go back to Add Sales</a>.</p>';
        saveBtn.disabled = true;
        printBtn.disabled = true;
        return;
    }

    var rowsHtml = (data.items || []).map(function (item) {
        return '<tr>' +
            '<td>' + escapeHtml(item.description) + '</td>' +
            '<td>' + item.quantity + '</td>' +
            '<td>' + currency(item.unitPrice) + '</td>' +
            '<td>' + currency(item.total) + '</td>' +
            '</tr>';
    }).join('');

    root.innerHTML =
        '<div class="receipt">' +
        '<div class="receipt-store">' +
        '<h1>PRESTIGE</h1>' +
        '<h2>SOFT DRINK STORE</h2>' +
        '<p class="receipt-tagline">Dealer in all kinds of soft drinks such as Coke, Fearless, Malt, Viju Milk, Yogurt etc. Both Wholesale and Retail.</p>' +
        '<p class="receipt-store-meta">Formal Dave Mercy Clinic, Idiagbo, Jebba, Kwara State &nbsp;•&nbsp; 08000000000</p>' +
        '</div>' +
        '<hr class="receipt-divider" />' +
        '<div class="receipt-meta">' +
        '<p><strong>Invoice No:</strong> <span>' + escapeHtml(data.invoiceNo) + '</span></p>' +
        '<p><strong>Date:</strong> <span>' + escapeHtml(data.dateDisplay) + '</span></p>' +
        '<p><strong>Customer:</strong> <span>' + escapeHtml(data.customerName) + '</span></p>' +
        '<p><strong>Phone:</strong> <span>' + escapeHtml(data.customerPhone || '—') + '</span></p>' +
        '<p><strong>Address:</strong> <span>' + escapeHtml(data.customerAddress || '—') + '</span></p>' +
        '</div>' +
        '<hr class="receipt-divider" />' +
        '<table class="receipt-table">' +
        '<thead><tr><th>Item</th><th>Qty</th><th>Unit</th><th>Total</th></tr></thead>' +
        '<tbody>' + rowsHtml + '</tbody>' +
        '</table>' +
        '<hr class="receipt-divider" />' +
        '<div class="receipt-total">' +
        '<div class="receipt-total-amount">' + currency(data.grandTotal) + '</div>' +
        '<div class="receipt-total-words">' + escapeHtml(data.amountInWords) + '</div>' +
        '</div>' +
        '<hr class="receipt-divider" />' +
        '<div class="receipt-note">RECEIVED GOODS IN GOOD CONDITION<br />NO REFUND AFTER PAYMENT</div>' +
        '<div class="receipt-thanks">THANK YOU</div>' +
        '<div class="receipt-signatures">' +
        '<div class="receipt-signature"><div class="sig-line"></div>Customer\'s Sign.</div>' +
        '<div class="receipt-signature"><div class="sig-line"></div>Manager\'s Sign.</div>' +
        '</div>' +
        '</div>';

    // ---------- Back ----------
    backBtn.addEventListener('click', function () {
        window.location.href = '/Sales/Create';
    });

    // ---------- Print ----------
    printBtn.addEventListener('click', function () {
        window.print();
    });

    // ---------- Save (capture receipt as image + persist to DB) ----------
    saveBtn.addEventListener('click', async function () {
        msgEl.textContent = '';
        msgEl.className = 'preview-msg no-print';

        if (typeof html2canvas !== 'function') {
            msgEl.textContent = 'Could not load the image capture library. Check your internet connection and try again.';
            msgEl.classList.add('error');
            return;
        }

        saveBtn.disabled = true;
        backBtn.disabled = true;
        saveBtn.classList.add('is-loading');

        try {
            var receiptEl = document.querySelector('.receipt');
            var canvas = await html2canvas(receiptEl, { backgroundColor: '#ffffff', scale: 2 });
            var imageDataUrl = canvas.toDataURL('image/png');

            var response = await fetch('/Sales/Save', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-CSRF-TOKEN': getAntiForgeryToken()
                },
                body: JSON.stringify({
                    customerName: data.customerName,
                    customerAddress: data.customerAddress,
                    customerPhone: data.customerPhone,
                    items: (data.items || []).map(function (i) {
                        return {
                            productId: i.productId,
                            description: i.description,
                            quantity: i.quantity,
                            unitPrice: i.unitPrice
                        };
                    }),
                    receiptImageBase64: imageDataUrl
                })
            });

            if (!response.ok) {
                var err = await response.json().catch(function () { return { message: 'Something went wrong.' }; });
                msgEl.textContent = err.message || 'Something went wrong.';
                msgEl.classList.add('error');
                saveBtn.disabled = false;
                backBtn.disabled = false;
                return;
            }

            sessionStorage.removeItem(STORAGE_KEY);
            msgEl.textContent = 'Sale saved successfully.';
            msgEl.classList.add('success');
            saveBtn.textContent = 'Saved ✓';
            backBtn.disabled = false;
            // Save stays disabled on success to prevent an accidental duplicate save.
        } catch (err) {
            msgEl.textContent = 'Network error. Please try again.';
            msgEl.classList.add('error');
            saveBtn.disabled = false;
            backBtn.disabled = false;
        } finally {
            saveBtn.classList.remove('is-loading');
        }
    });
});