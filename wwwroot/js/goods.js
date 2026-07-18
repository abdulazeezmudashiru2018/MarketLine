document.addEventListener('DOMContentLoaded', function () {
    var grid = document.getElementById('goodsGrid');
    var emptyMsg = document.getElementById('goodsEmptyMsg');

    var itemModalOverlay = document.getElementById('itemModalOverlay');
    var itemModalTitle = document.getElementById('itemModalTitle');
    var itemForm = document.getElementById('itemForm');
    var itemIdInput = document.getElementById('itemId');
    var itemNameInput = document.getElementById('itemName');
    var itemPriceInput = document.getElementById('itemPrice');
    var itemImageInput = document.getElementById('itemImage');
    var itemFormError = document.getElementById('itemFormError');
    var imageHint = document.getElementById('imageHint');
    var itemBarcodeInput = document.getElementById('itemBarcode');

    var deleteModalOverlay = document.getElementById('deleteModalOverlay');
    var deleteItemName = document.getElementById('deleteItemName');
    var pendingDeleteId = null;

    function getAntiForgeryToken() {
        var el = document.querySelector('input[name="__RequestVerificationToken"]');
        return el ? el.value : '';
    }

    function currency(amount) {
        return '₦' + new Intl.NumberFormat().format(Math.round(amount));
    }

    function hideEmptyMessage() {
        if (emptyMsg) { emptyMsg.style.display = 'none'; }
    }

    function showEmptyMessageIfNeeded() {
        if (grid.querySelectorAll('.goods-card').length === 0 && emptyMsg) {
            emptyMsg.style.display = 'block';
        }
    }

    function iconEditSvg() {
        return '<svg viewBox="0 0 24 24"><path d="M4 20h4l10-10-4-4L4 16v4Z" /><path d="m14.5 5.5 4 4" /></svg>';
    }
    function iconDeleteSvg() {
        return '<svg viewBox="0 0 24 24"><path d="M4 7h16" /><path d="M9 7V4h6v3" /><path d="M6 7l1 13h10l1-13" /><path d="M10 11v6M14 11v6" /></svg>';
    }
    function iconPlaceholderSvg() {
        return '<svg viewBox="0 0 24 24" class="goods-placeholder-icon"><path d="M4 16.5 8.5 12l3 3 5-6L21 16.5" /><rect x="3" y="4" width="18" height="16" rx="2.5" /></svg>';
    }

    function buildCard(product) {
        var card = document.createElement('div');
        card.className = 'goods-card';
        card.dataset.id = product.id;
        card.dataset.barcode = product.barcode || '';
        var imageHtml = product.imagePath
            ? '<img src="' + product.imagePath + '" alt="' + escapeHtml(product.name) + '" />'
            : iconPlaceholderSvg();

        card.innerHTML =
            '<div class="goods-card-actions">' +
                '<button type="button" class="icon-btn edit-btn" title="Edit" aria-label="Edit item">' + iconEditSvg() + '</button>' +
                '<button type="button" class="icon-btn delete-btn" title="Delete" aria-label="Delete item">' + iconDeleteSvg() + '</button>' +
            '</div>' +
            '<div class="goods-card-image">' + imageHtml + '</div>' +
            '<div class="goods-card-body">' +
                '<div class="goods-card-title"></div>' +
                '<div class="goods-card-price"></div>' +
            '</div>';

        card.querySelector('.goods-card-title').textContent = product.name;
        card.querySelector('.goods-card-price').textContent = currency(product.price);

        return card;
    }

    function escapeHtml(str) {
        var div = document.createElement('div');
        div.textContent = str;
        return div.innerHTML;
    }

    function updateCard(card, product) {
        card.querySelector('.goods-card-title').textContent = product.name;
        card.querySelector('.goods-card-price').textContent = currency(product.price);
        card.dataset.barcode = product.barcode || '';
        if (product.imagePath) {
            var imgWrap = card.querySelector('.goods-card-image');
            imgWrap.innerHTML = '<img src="' + product.imagePath + '?t=' + Date.now() + '" alt="' + escapeHtml(product.name) + '" />';
        }
    }

    // ---------- Add / Edit modal ----------

    function openAddModal() {
        itemModalTitle.textContent = 'Add New Item';
        itemIdInput.value = '0';
        itemNameInput.value = '';
        itemPriceInput.value = '';
        itemImageInput.value = '';
        imageHint.textContent = '(optional)';
        itemFormError.textContent = '';
        itemModalOverlay.classList.add('open');
        itemNameInput.focus();
        itemBarcodeInput.value = '';                      
        itemBarcodeInput.value = card.dataset.barcode || '';

    }

    function openEditModal(card) {
        var title = card.querySelector('.goods-card-title').textContent;
        var priceText = card.querySelector('.goods-card-price').textContent.replace(/[^0-9.]/g, '');

        itemModalTitle.textContent = 'Edit Item';
        itemIdInput.value = card.dataset.id;
        itemNameInput.value = title;
        itemPriceInput.value = priceText;
        itemImageInput.value = '';
        imageHint.textContent = '(leave empty to keep current image)';
        itemFormError.textContent = '';
        itemModalOverlay.classList.add('open');
        itemNameInput.focus();
        itemBarcodeInput.value = '';
        itemBarcodeInput.value = card.dataset.barcode || '';
    }

    function closeItemModal() {
        itemModalOverlay.classList.remove('open');
    }

    document.getElementById('btnOpenAdd').addEventListener('click', openAddModal);
    document.getElementById('btnCancelItem').addEventListener('click', closeItemModal);
    itemModalOverlay.addEventListener('click', function (e) {
        if (e.target === itemModalOverlay) closeItemModal();
    });

    itemForm.addEventListener('submit', async function (e) {
        e.preventDefault();
        itemFormError.textContent = '';

        var id = parseInt(itemIdInput.value, 10) || 0;
        var isEdit = id > 0;

        var name = itemNameInput.value.trim();
        var price = parseFloat(itemPriceInput.value);

        if (!name) { itemFormError.textContent = 'Please enter an item name.'; return; }
        if (isNaN(price) || price < 0) { itemFormError.textContent = 'Please enter a valid price.'; return; }

        var formData = new FormData();
        formData.append('Id', id);
        formData.append('Name', name);
        formData.append('Price', price);
        formData.append('__RequestVerificationToken', getAntiForgeryToken());
        formData.append('Barcode', itemBarcodeInput.value.trim());
        if (itemImageInput.files[0]) {
            formData.append('Image', itemImageInput.files[0]);
        }

        var saveBtn = document.getElementById('btnSaveItem');
        saveBtn.disabled = true;
        saveBtn.textContent = 'Saving…';

        try {
            var url = isEdit ? '/Goods/Edit' : '/Goods/Create';
            var response = await fetch(url, { method: 'POST', body: formData });

            if (!response.ok) {
                var err = await response.json().catch(function () { return { message: 'Something went wrong.' }; });
                itemFormError.textContent = err.message || 'Something went wrong.';
                return;
            }

            var product = await response.json();
            hideEmptyMessage();

            if (isEdit) {
                var existingCard = grid.querySelector('.goods-card[data-id="' + product.id + '"]');
                if (existingCard) updateCard(existingCard, product);
            } else {
                grid.insertBefore(buildCard(product), grid.firstChild);
            }

            closeItemModal();
        } catch (err) {
            itemFormError.textContent = 'Network error. Please try again.';
        } finally {
            saveBtn.disabled = false;
            saveBtn.textContent = 'Save';
        }
    });

    // ---------- Delete confirmation ----------

    function openDeleteModal(card) {
        pendingDeleteId = card.dataset.id;
        deleteItemName.textContent = card.querySelector('.goods-card-title').textContent;
        deleteModalOverlay.classList.add('open');
    }

    function closeDeleteModal() {
        deleteModalOverlay.classList.remove('open');
        pendingDeleteId = null;
    }

    document.getElementById('btnCancelDelete').addEventListener('click', closeDeleteModal);
    deleteModalOverlay.addEventListener('click', function (e) {
        if (e.target === deleteModalOverlay) closeDeleteModal();
    });

    document.getElementById('btnConfirmDelete').addEventListener('click', async function () {
        if (!pendingDeleteId) return;
        var confirmBtn = this;
        confirmBtn.disabled = true;
        confirmBtn.textContent = 'Deleting…';

        try {
            var formData = new FormData();
            formData.append('id', pendingDeleteId);
            formData.append('__RequestVerificationToken', getAntiForgeryToken());

            var response = await fetch('/Goods/Delete', { method: 'POST', body: formData });

            if (response.ok) {
                var card = grid.querySelector('.goods-card[data-id="' + pendingDeleteId + '"]');
                if (card) card.remove();
                showEmptyMessageIfNeeded();
                closeDeleteModal();
            } else {
                alert('Could not delete this item. Please try again.');
            }
        } catch (err) {
            alert('Network error. Please try again.');
        } finally {
            confirmBtn.disabled = false;
            confirmBtn.textContent = 'Delete';
        }
    });

    // ---------- Delegate edit/delete clicks from cards ----------

    grid.addEventListener('click', function (e) {
        var editBtn = e.target.closest('.edit-btn');
        var deleteBtn = e.target.closest('.delete-btn');
        if (editBtn) {
            openEditModal(editBtn.closest('.goods-card'));
        } else if (deleteBtn) {
            openDeleteModal(deleteBtn.closest('.goods-card'));
        }
    });
});
