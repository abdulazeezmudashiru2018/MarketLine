document.addEventListener('DOMContentLoaded', function () {

    // ---------- shared helpers ----------

    function currency(amount) {
        return '₦' + new Intl.NumberFormat().format(Math.round(amount));
    }

    function escapeHtml(str) {
        var div = document.createElement('div');
        div.textContent = str;
        return div.innerHTML;
    }

    function getAntiForgeryToken() {
        var el = document.querySelector('input[name="__RequestVerificationToken"]');
        return el ? el.value : '';
    }

    // ---------- number -> words (for "Amount in words") ----------

    var ONES = ['', 'One', 'Two', 'Three', 'Four', 'Five', 'Six', 'Seven', 'Eight', 'Nine',
        'Ten', 'Eleven', 'Twelve', 'Thirteen', 'Fourteen', 'Fifteen', 'Sixteen', 'Seventeen', 'Eighteen', 'Nineteen'];
    var TENS = ['', '', 'Twenty', 'Thirty', 'Forty', 'Fifty', 'Sixty', 'Seventy', 'Eighty', 'Ninety'];

    function threeDigitsToWords(n) {
        var str = '';
        if (n >= 100) {
            str += ONES[Math.floor(n / 100)] + ' Hundred';
            n = n % 100;
            if (n > 0) str += ' and ';
        }
        if (n >= 20) {
            str += TENS[Math.floor(n / 10)];
            if (n % 10 > 0) str += '-' + ONES[n % 10];
        } else if (n > 0) {
            str += ONES[n];
        }
        return str;
    }

    function numberToWords(num) {
        num = Math.floor(num);
        if (num === 0) return 'Zero';

        var groups = [
            { value: 1000000000000, label: 'Trillion' },
            { value: 1000000000, label: 'Billion' },
            { value: 1000000, label: 'Million' },
            { value: 1000, label: 'Thousand' },
            { value: 1, label: '' }
        ];

        var parts = [];
        var remaining = num;

        groups.forEach(function (g) {
            var chunk = Math.floor(remaining / g.value);
            if (chunk > 0) {
                parts.push(threeDigitsToWords(chunk) + (g.label ? ' ' + g.label : ''));
                remaining = remaining % g.value;
            }
        });

        return parts.join(' ');
    }

    function amountInWords(total) {
        var naira = Math.floor(total);
        if (naira <= 0) return 'Zero Naira Only';
        return numberToWords(naira) + ' Naira Only';
    }

    // ================================================================
    // Invoice table: rows, stepper, totals
    // ================================================================

    var invoiceBody = document.getElementById('invoiceBody');
    var grandTotalEl = document.getElementById('grandTotal');
    var amountWordsEl = document.getElementById('amountInWords');
    var rowCounter = 0;

    function iconDeleteSvg() {
        return '<svg viewBox="0 0 24 24"><path d="M4 7h16" /><path d="M9 7V4h6v3" /><path d="M6 7l1 13h10l1-13" /><path d="M10 11v6M14 11v6" /></svg>';
    }

    function buildRow(opts) {
       
        rowCounter++;

        var tr = document.createElement('tr');
        if (opts.productId) tr.dataset.productId = opts.productId;
        tr.innerHTML =
            '<td class="row-index">' + rowCounter + '</td>' +
            '<td class="desc-cell"><input type="text" class="desc-input" /></td>' +
            '<td class="qty-cell">' +
            '<div class="qty-stepper">' +
            '<input type="number" class="qty-input" min="0" max="999" step="1" />' +
            '<div class="qty-arrows">' +
            '<button type="button" class="qty-up" aria-label="Increase quantity">▲</button>' +
            '<button type="button" class="qty-down" aria-label="Decrease quantity">▼</button>' +
            '</div>' +
            '</div>' +
            '<div class="qty-limit-msg">Max quantity is 999</div>' +
            '</td>' +
            '<td class="price-cell"><input type="number" class="price-input" min="0" step="0.01" /></td>' +
            '<td class="total-price-cell">₦0</td>' +
            '<td><button type="button" class="delete-row-btn" title="Remove item" aria-label="Remove item">' + iconDeleteSvg() + '</button></td>';
        var descInput = tr.querySelector('.desc-input');
        var qtyInput = tr.querySelector('.qty-input');
        var priceInput = tr.querySelector('.price-input');

        descInput.value = opts.description || '';
        qtyInput.value = opts.quantity != null ? opts.quantity : 0;
        priceInput.value = opts.unitPrice != null ? opts.unitPrice : 0;

        var qtyLimitMsg = tr.querySelector('.qty-limit-msg');
        var qtyLimitTimer = null;
        var MAX_QTY = 999;

        // Rejects any quantity above 999: clamps the value back down and
        // shows a brief "exceed input limit" warning under the stepper.
        function enforceQtyLimit() {
            var raw = parseFloat(qtyInput.value);
            if (!isNaN(raw) && raw > MAX_QTY) {
                qtyInput.value = MAX_QTY;
                qtyInput.classList.add('qty-input-error');
                qtyLimitMsg.classList.add('show');
                clearTimeout(qtyLimitTimer);
                qtyLimitTimer = setTimeout(function () {
                    qtyInput.classList.remove('qty-input-error');
                    qtyLimitMsg.classList.remove('show');
                }, 2200);
                return true;
            }
            return false;
        }

        function updateRowTotal() {
            enforceQtyLimit();
            var qty = parseFloat(qtyInput.value) || 0;
            var price = parseFloat(priceInput.value) || 0;
            tr.querySelector('.total-price-cell').textContent = currency(qty * price);
            recalcGrandTotal();
        }

        
        async function tryAutoFillPriceFromCatalog() {
            var desc = descInput.value.trim();
            var currentPrice = parseFloat(priceInput.value) || 0;
            if (!desc || currentPrice > 0 || tr.dataset.priceLookupBusy === '1') return;

            tr.dataset.priceLookupBusy = '1';
            try {
                var response = await fetch('/Goods/Search?q=' + encodeURIComponent(desc));
                if (response.ok) {
                    var matches = await response.json();
                    var exact = matches.find(function (m) {
                        return m.name.toLowerCase() === desc.toLowerCase();
                    });
                    var match = exact || (matches.length === 1 ? matches[0] : null);
                    if (match) {
                        priceInput.value = match.price;
                        tr.dataset.productId = match.id;
                        updateRowTotal();
                    }
                }
            } catch (err) {
                /* silent - price lookup is a convenience, not critical */
            } finally {
                tr.dataset.priceLookupBusy = '0';
            }
        }

       
        descInput.addEventListener('input', function () {
            qtyInput.value = 0;
            priceInput.value = 0;
            delete tr.dataset.productId;
            updateRowTotal();
        });

        qtyInput.addEventListener('input', function () {
            tryAutoFillPriceFromCatalog();
            updateRowTotal();
        });
        priceInput.addEventListener('input', updateRowTotal);

        tr.querySelector('.qty-up').addEventListener('click', function () {
            qtyInput.value = (parseFloat(qtyInput.value) || 0) + 1;
            tryAutoFillPriceFromCatalog();
            updateRowTotal();
        });
        tr.querySelector('.qty-down').addEventListener('click', function () {
            var next = (parseFloat(qtyInput.value) || 0) - 1;
            qtyInput.value = next < 0 ? 0 : next;
            tryAutoFillPriceFromCatalog();
            updateRowTotal();
        });

        tr.querySelector('.delete-row-btn').addEventListener('click', function () {
            tr.remove();
            renumberRows();
            recalcGrandTotal();
        });

        updateRowTotal();
        return tr;
    }

    function addInvoiceRowFromItem(item) {
        // If this product is already in the invoice, just bump its
        // quantity instead of creating a duplicate row.
        var existingRow = invoiceBody.querySelector('tr[data-product-id="' + item.id + '"]');
        if (existingRow) {
            var qtyInput = existingRow.querySelector('.qty-input');
            qtyInput.value = (parseFloat(qtyInput.value) || 0) + 1;
            qtyInput.dispatchEvent(new Event('input'));

            // brief highlight so it's obvious which row was updated
            existingRow.classList.add('row-flash');
            setTimeout(function () { existingRow.classList.remove('row-flash'); }, 500);
            return;
        }

        var tr = buildRow({
            productId: item.id,
            description: item.name,
            quantity: 1,
            unitPrice: item.price
        });
        invoiceBody.appendChild(tr);
    }
    function addManualRow() {
        var tr = buildRow({
            description: '',
            quantity: 0,
            unitPrice: 0
        });
        invoiceBody.appendChild(tr);
        tr.querySelector('.desc-input').focus();
    }

    function renumberRows() {
        rowCounter = 0;
        invoiceBody.querySelectorAll('tr').forEach(function (row) {
            rowCounter++;
            row.querySelector('.row-index').textContent = rowCounter;
        });
    }

    function recalcGrandTotal() {
        var total = 0;
        invoiceBody.querySelectorAll('tr').forEach(function (row) {
            var qty = parseFloat(row.querySelector('.qty-input').value) || 0;
            var price = parseFloat(row.querySelector('.price-input').value) || 0;
            total += qty * price;
        });
        grandTotalEl.textContent = currency(total);
        grandTotalEl.title = currency(total);
        autoFitGrandTotal();
        amountWordsEl.textContent = amountInWords(total);
        return total;
    }

    // Shrinks the grand total's font size until it fits inside the
    // total-box, so large amounts are never clipped or hidden.
    function autoFitGrandTotal() {
        var container = grandTotalEl.parentElement;
        if (!container) return;

        var maxSize = 21.6; // ~1.35rem
        var minSize = 11;
        var size = maxSize;
        grandTotalEl.style.fontSize = size + 'px';

        var available = container.clientWidth - 20; // account for padding
        while (grandTotalEl.scrollWidth > available && size > minSize) {
            size -= 1;
            grandTotalEl.style.fontSize = size + 'px';
        }
    }

    document.getElementById('btnAddManualRow').addEventListener('click', addManualRow);

    // ================================================================
    // Goods drawer (slide-out panel + live search)
    // ================================================================

    var overlay = document.getElementById('goodsDrawerOverlay');
    var openGoodsBtn = document.getElementById('btnOpenGoods');
    var closeGoodsBtn = document.getElementById('btnCloseGoods');
    var searchInput = document.getElementById('goodsSearchInput');
    var listEl = document.getElementById('goodsDrawerList');
    var debounceTimer = null;

    function openDrawer() {
        overlay.classList.add('open');
        searchInput.value = '';
        loadGoods('');
        setTimeout(function () { searchInput.focus(); }, 150);
    }
    function closeDrawer() {
        overlay.classList.remove('open');
    }

    openGoodsBtn.addEventListener('click', openDrawer);
    closeGoodsBtn.addEventListener('click', closeDrawer);
    overlay.addEventListener('click', function (e) { if (e.target === overlay) closeDrawer(); });

    searchInput.addEventListener('input', function () {
        clearTimeout(debounceTimer);
        var q = this.value;
        debounceTimer = setTimeout(function () { loadGoods(q); }, 250);
    });

    async function loadGoods(q) {
        listEl.innerHTML = '<div class="goods-drawer-empty">Loading…</div>';
        try {
            var response = await fetch('/Goods/Search?q=' + encodeURIComponent(q || ''));
            if (!response.ok) { listEl.innerHTML = '<div class="goods-drawer-empty">Could not load items.</div>'; return; }
            var items = await response.json();
            renderGoodsList(items);
        } catch (err) {
            listEl.innerHTML = '<div class="goods-drawer-empty">Network error.</div>';
        }
    }

    function renderGoodsList(items) {
        if (!items.length) { listEl.innerHTML = '<div class="goods-drawer-empty">No items found.</div>'; return; }
        listEl.innerHTML = '';
        items.forEach(function (item) {
            var row = document.createElement('div');
            row.className = 'goods-drawer-item';
            var imageHtml = item.imagePath
                ? '<img src="' + item.imagePath + '" alt="' + escapeHtml(item.name) + '" />'
                : '<div class="goods-drawer-item-placeholder"></div>';
            row.innerHTML =
                imageHtml +
                '<div class="goods-drawer-item-info">' +
                '<div class="goods-drawer-item-name"></div>' +
                '<div class="goods-drawer-item-price"></div>' +
                '</div>';
            row.querySelector('.goods-drawer-item-name').textContent = item.name;
            row.querySelector('.goods-drawer-item-price').textContent = currency(item.price);
            row.addEventListener('click', function () {
                addInvoiceRowFromItem(item);

                // brief visual confirmation since the drawer stays open now
                row.classList.add('item-added-flash');
                setTimeout(function () { row.classList.remove('item-added-flash'); }, 400);
            });
            listEl.appendChild(row);
        });
    }


    var customerNameInput = document.getElementById('customerName');
    var customerAddressInput = document.getElementById('customerAddress');
    var customerPhoneInput = document.getElementById('customerPhone');
    var nameSuggestionsEl = document.getElementById('customerSuggestions');
    var phoneSuggestionsEl = document.getElementById('phoneSuggestions');
    var nameDebounce = null;
    var phoneDebounce = null;

    function fillCustomerFields(c, suggestionsEl) {
        customerNameInput.value = c.name;
        customerAddressInput.value = c.address || '';
        customerPhoneInput.value = c.phone || '';
        suggestionsEl.classList.remove('open');
    }

    function renderSuggestions(customers, suggestionsEl) {
        if (!customers.length) { suggestionsEl.innerHTML = ''; suggestionsEl.classList.remove('open'); return; }
        suggestionsEl.innerHTML = '';
        customers.forEach(function (c) {
            var item = document.createElement('div');
            item.className = 'customer-suggestion-item';
            item.textContent = c.name + (c.phone ? ' — ' + c.phone : '');
            item.addEventListener('click', function () { fillCustomerFields(c, suggestionsEl); });
            suggestionsEl.appendChild(item);
        });
        suggestionsEl.classList.add('open');
    }

    async function loadSuggestions(url, suggestionsEl) {
        try {
            var response = await fetch(url);
            if (!response.ok) return;
            var customers = await response.json();
            renderSuggestions(customers, suggestionsEl);
        } catch (err) { /* silent - autocomplete is a convenience, not critical */ }
    }

    // ---- Name field ----
    customerNameInput.addEventListener('input', function () {
        var q = this.value;
        clearTimeout(nameDebounce);
        if (q.trim().length < 2) { nameSuggestionsEl.innerHTML = ''; nameSuggestionsEl.classList.remove('open'); return; }
        nameDebounce = setTimeout(function () {
            loadSuggestions('/Customers/Search?q=' + encodeURIComponent(q), nameSuggestionsEl);
        }, 250);
    });

    // ---- Phone field ----
    customerPhoneInput.addEventListener('input', function () {
        var q = this.value;
        clearTimeout(phoneDebounce);
        if (q.trim().length < 2) { phoneSuggestionsEl.innerHTML = ''; phoneSuggestionsEl.classList.remove('open'); return; }
        phoneDebounce = setTimeout(function () {
            loadSuggestions('/Customers/SearchByPhone?q=' + encodeURIComponent(q), phoneSuggestionsEl);
        }, 250);
    });

    document.addEventListener('click', function (e) {
        if (!nameSuggestionsEl.contains(e.target) && e.target !== customerNameInput) {
            nameSuggestionsEl.classList.remove('open');
        }
        if (!phoneSuggestionsEl.contains(e.target) && e.target !== customerPhoneInput) {
            phoneSuggestionsEl.classList.remove('open');
        }
    });

    // ================================================================
    // Camera barcode scanner

    // ================================================================
    // Camera barcode scanner
    // ================================================================

    var cameraOverlay = document.getElementById('cameraModalOverlay');
    var chooseStep = document.getElementById('cameraChooseStep');
    var scanStep = document.getElementById('cameraScanStep');
    var video = document.getElementById('cameraVideo');
    var cameraStatus = document.getElementById('cameraStatus');
    var currentStream = null;
    var scanTimer = null;

    document.getElementById('btnOpenCamera').addEventListener('click', function () {
        chooseStep.style.display = 'block';
        scanStep.style.display = 'none';
        cameraOverlay.classList.add('open');
    });
    document.getElementById('btnCancelCamera').addEventListener('click', closeCameraModal);
    cameraOverlay.addEventListener('click', function (e) { if (e.target === cameraOverlay) closeCameraModal(); });
    document.getElementById('btnStopCamera').addEventListener('click', closeCameraModal);

    document.getElementById('btnUseFrontCamera').addEventListener('click', function () { startCamera('user'); });
    document.getElementById('btnUseBackCamera').addEventListener('click', function () { startCamera('environment'); });

    function closeCameraModal() {
        cameraOverlay.classList.remove('open');
        stopScanning();
    }

    function stopScanning() {
        if (scanTimer) { clearInterval(scanTimer); scanTimer = null; }
        if (currentStream) {
            currentStream.getTracks().forEach(function (t) { t.stop(); });
            currentStream = null;
        }
    }

    async function startCamera(facingMode) {
        chooseStep.style.display = 'none';
        scanStep.style.display = 'block';
        cameraStatus.textContent = 'Starting camera…';

        try {
            currentStream = await navigator.mediaDevices.getUserMedia({
                video: { facingMode: facingMode }
            });
            video.srcObject = currentStream;
            await video.play();
        } catch (err) {
            cameraStatus.textContent = 'Could not access the camera. Check permissions and try again.';
            return;
        }

        if (!('BarcodeDetector' in window)) {
            cameraStatus.textContent = 'Barcode scanning isn\'t supported in this browser. Try Chrome or Edge, or add the item via search instead.';
            return;
        }

        var detector = new window.BarcodeDetector();
        cameraStatus.textContent = 'Looking for a barcode…';

        scanTimer = setInterval(async function () {
            try {
                var codes = await detector.detect(video);
                if (codes.length > 0) {
                    var value = codes[0].rawValue;
                    cameraStatus.textContent = 'Found: ' + value + ' — checking catalog…';
                    await lookupBarcode(value);
                }
            } catch (err) { /* keep scanning silently on transient frame errors */ }
        }, 500);
    }

    async function lookupBarcode(code) {
        try {
            var response = await fetch('/Goods/ByBarcode?code=' + encodeURIComponent(code));
            if (response.ok) {
                var product = await response.json();
                addInvoiceRowFromItem(product);
                cameraStatus.textContent = 'Added "' + product.name + '" to the invoice.';
                closeCameraModal();
            } else {
                cameraStatus.textContent = 'No catalog item matches this barcode. Still scanning…';
            }
        } catch (err) {
            cameraStatus.textContent = 'Network error checking barcode. Still scanning…';
        }
    }

  
    var STORAGE_KEY = 'marketline_pending_sale';
    var previewBtn = document.getElementById('btnPreviewSale');
    var previewMsg = document.getElementById('saveMsg');

    function generateInvoiceNo() {
        var d = new Date();
        var y = d.getFullYear();
        var m = String(d.getMonth() + 1).padStart(2, '0');
        var day = String(d.getDate()).padStart(2, '0');
        var rand = Math.floor(1000 + Math.random() * 9000);
        return 'INV-' + y + m + day + '-' + rand;
    }

    previewBtn.addEventListener('click', function () {
        previewMsg.textContent = '';
        previewMsg.className = 'invoice-save-msg';

        var customerName = customerNameInput.value.trim();
        if (!customerName) {
            previewMsg.textContent = 'Please enter the customer\'s name.';
            previewMsg.classList.add('error');
            return;
        }

        var items = [];
        invoiceBody.querySelectorAll('tr').forEach(function (row) {
            var description = row.querySelector('.desc-input').value.trim();
            var quantity = parseFloat(row.querySelector('.qty-input').value) || 0;
            var unitPrice = parseFloat(row.querySelector('.price-input').value) || 0;
            if (description && quantity > 0) {
                items.push({
                    productId: row.dataset.productId ? parseInt(row.dataset.productId, 10) : null,
                    description: description,
                    quantity: quantity,
                    unitPrice: unitPrice,
                    total: quantity * unitPrice
                });
            }
        });

        if (!items.length) {
            previewMsg.textContent = 'Add at least one item before previewing the sale.';
            previewMsg.classList.add('error');
            return;
        }

        // Keep the same invoice number if we're re-previewing after
        // going Back to edit, instead of generating a new one each time.
        var invoiceNo = generateInvoiceNo();
        var existingRaw = sessionStorage.getItem(STORAGE_KEY);
        if (existingRaw) {
            try {
                var existing = JSON.parse(existingRaw);
                if (existing.invoiceNo) invoiceNo = existing.invoiceNo;
            } catch (e) { /* ignore malformed cache */ }
        }

        var grandTotal = recalcGrandTotal();

        var payload = {
            invoiceNo: invoiceNo,
            dateDisplay: dateTimeEl ? dateTimeEl.textContent : '',
            customerName: customerName,
            customerAddress: customerAddressInput.value.trim(),
            customerPhone: customerPhoneInput.value.trim(),
            items: items,
            grandTotal: grandTotal,
            amountInWords: amountInWords(grandTotal)
        };

        sessionStorage.setItem(STORAGE_KEY, JSON.stringify(payload));
        window.location.href = '/Sales/Preview';
    });

    // Restores customer details + line items from a pending preview
    // (e.g. after clicking "Back" on the Preview page to keep editing).
    function restorePendingSale() {
        var raw = sessionStorage.getItem(STORAGE_KEY);
        if (!raw) return false;

        var data;
        try { data = JSON.parse(raw); } catch (e) { return false; }

        customerNameInput.value = data.customerName || '';
        customerAddressInput.value = data.customerAddress || '';
        customerPhoneInput.value = data.customerPhone || '';

        if (data.dateDisplay && dateTimeEl) {
            dateTimeEl.textContent = data.dateDisplay;
        }

        (data.items || []).forEach(function (item) {
            var tr = buildRow({
                productId: item.productId,
                description: item.description,
                quantity: item.quantity,
                unitPrice: item.unitPrice
            });
            invoiceBody.appendChild(tr);
        });

        return true;
    }

    // ================================================================
    // Invoice date/time stamp (shown opposite "Customer Name")
    // ================================================================
    var dateTimeEl = document.getElementById('invoiceDateTime');
    var restored = restorePendingSale();
    if (!restored && dateTimeEl) {
        dateTimeEl.textContent = new Date().toLocaleString(undefined, {
            dateStyle: 'medium',
            timeStyle: 'short'
        });
    }

    recalcGrandTotal();
});
