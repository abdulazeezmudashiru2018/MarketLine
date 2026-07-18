document.addEventListener('DOMContentLoaded', function () {
    var searchInput = document.getElementById('customerSearchInput');
    var listEl = document.getElementById('customersList');
    var noMatchEl = document.getElementById('customersNoMatch');
    var cards = Array.prototype.slice.call(listEl.querySelectorAll('.customer-card'));

    var overlay = document.getElementById('receiptsOverlay');
    var closeBtn = document.getElementById('btnCloseReceipts');
    var nameEl = document.getElementById('receiptsCustomerName');
    var receiptsListEl = document.getElementById('receiptsList');

    function currency(amount) {
        return '₦' + new Intl.NumberFormat().format(Math.round(amount));
    }
    function escapeHtml(str) {
        var div = document.createElement('div');
        div.textContent = str == null ? '' : String(str);
        return div.innerHTML;
    }

    // ---------- Search by name or phone ----------
    if (searchInput) {
        searchInput.addEventListener('input', function () {
            var term = this.value.trim().toLowerCase();
            var visibleCount = 0;

            cards.forEach(function (card) {
                var matches = !term ||
                    card.dataset.name.indexOf(term) !== -1 ||
                    card.dataset.phone.toLowerCase().indexOf(term) !== -1;
                card.style.display = matches ? '' : 'none';
                if (matches) visibleCount++;
            });

            if (noMatchEl) {
                noMatchEl.style.display = (term && visibleCount === 0) ? 'block' : 'none';
            }
        });
    }

    // ---------- Receipts panel ----------
    function openReceipts() { overlay.classList.add('open'); }
    function closeReceipts() { overlay.classList.remove('open'); }

    closeBtn.addEventListener('click', closeReceipts);
    overlay.addEventListener('click', function (e) {
        if (e.target === overlay) closeReceipts();
    });

    cards.forEach(function (card) {
        card.addEventListener('click', function () {
            var id = card.dataset.id;
            nameEl.textContent = card.querySelector('.customer-name').textContent;
            receiptsListEl.innerHTML = '<div class="receipts-empty">Loading…</div>';
            openReceipts();
            loadReceipts(id);
        });
    });

    async function loadReceipts(id) {
        try {
            var response = await fetch('/Customers/Receipts/' + id);
            if (!response.ok) {
                receiptsListEl.innerHTML = '<div class="receipts-empty">Could not load receipts.</div>';
                return;
            }
            var data = await response.json();
            renderReceipts(data.receipts || []);
        } catch (err) {
            receiptsListEl.innerHTML = '<div class="receipts-empty">Network error.</div>';
        }
    }

    function renderReceipts(receipts) {
        if (!receipts.length) {
            receiptsListEl.innerHTML = '<div class="receipts-empty">No receipts yet for this customer.</div>';
            return;
        }

        receiptsListEl.innerHTML = receipts.map(function (r) {
            var dateStr = new Date(r.createdAt).toLocaleString(undefined, { dateStyle: 'medium', timeStyle: 'short' });
            var itemsHtml = (r.items || []).map(function (i) {
                return '<li>' + escapeHtml(i.description) + ' <span>x' + i.quantity + '</span> <strong>' + currency(i.totalPrice) + '</strong></li>';
            }).join('');

            var imageHtml = r.receiptImagePath
                ? '<a class="receipt-image-link" href="' + r.receiptImagePath + '" target="_blank" rel="noopener">View receipt image</a>'
                : '';

            return (
                '<div class="receipt-card">' +
                '<div class="receipt-card-head">' +
                '<span class="receipt-card-date">' + dateStr + '</span>' +
                '<span class="receipt-card-total">' + currency(r.totalAmount) + '</span>' +
                '</div>' +
                '<ul class="receipt-card-items">' + itemsHtml + '</ul>' +
                imageHtml +
                '</div>'
            );
        }).join('');
    }
});