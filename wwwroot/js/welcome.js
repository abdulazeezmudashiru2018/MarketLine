document.addEventListener('DOMContentLoaded', function () {

    function slotContainerId(slot) {
        return slot === 'left' ? 'heroMediaLeftInner' : 'heroMediaRightInner';
    }
    function slotPlaceholderId(slot) {
        return slot === 'left' ? 'heroMediaLeftPlaceholder' : 'heroMediaRightPlaceholder';
    }
    function otherSlot(slot) {
        return slot === 'left' ? 'right' : 'left';
    }

    // ================================================================
    // Sound: browsers block autoplay-with-sound until the visitor has
    // interacted with the page at least once. The moment that happens
    // (a click/tap/keypress anywhere), audio unlocks for the rest of
    // the visit and every video that slides in from then on plays with
    // sound automatically — no button, no further clicks needed.
    // Only one side is ever audible at a time.
    // ================================================================
    var audioUnlocked = false;

    function claimSound(slot, video) {
        if (!audioUnlocked) {
            video.muted = true;
            video.play().catch(function () { });
            return;
        }

        // Silence whatever the other side is currently playing so only
        // one side is ever audible at once.
        var otherContainer = document.getElementById(slotContainerId(otherSlot(slot)));
        var otherActive = otherContainer && otherContainer.querySelector('.hero-slide.active');
        var otherVideo = otherActive && otherActive.querySelector('video');
        if (otherVideo) otherVideo.muted = true;

        video.muted = false;
        video.play().catch(function () {
            video.muted = true;
            video.play().catch(function () { });
        });
    }

    function unlockAudioOnce() {
        if (audioUnlocked) return;
        audioUnlocked = true;

        // Give sound immediately to whichever video happens to be
        // showing right now, rather than waiting for the next 5s tick.
        ['left', 'right'].forEach(function (slot) {
            var container = document.getElementById(slotContainerId(slot));
            var activeSlide = container && container.querySelector('.hero-slide.active');
            var video = activeSlide && activeSlide.querySelector('video');
            if (video) claimSound(slot, video);
        });
    }

    document.addEventListener('click', unlockAudioOnce, { once: true });
    document.addEventListener('touchstart', unlockAudioOnce, { once: true });
    document.addEventListener('keydown', unlockAudioOnce, { once: true });

    // ================================================================
    // Auto-advancing slideshow (every 5 seconds) for each hero slot
    // ================================================================
    function startSlideshow(innerId, slot) {
        var container = document.getElementById(innerId);
        if (!container) return;

        var initialActive = container.querySelector('.hero-slide.active');
        var initVideo = initialActive && initialActive.querySelector('video');
        if (initVideo) claimSound(slot, initVideo);

        setInterval(function () {
            // Re-query live each tick so newly added/removed slides
            // (via the manage modal) are picked up automatically.
            var slides = container.querySelectorAll('.hero-slide');
            if (slides.length <= 1) return;

            var currentIndex = -1;
            slides.forEach(function (slide, i) {
                if (slide.classList.contains('active')) currentIndex = i;
            });

            var nextIndex = (currentIndex + 1) % slides.length;

            slides.forEach(function (slide, i) {
                if (i === nextIndex) {
                    slide.classList.add('active');
                    var video = slide.querySelector('video');
                    if (video) {
                        video.currentTime = 0;
                        claimSound(slot, video);
                    }
                } else {
                    slide.classList.remove('active');
                    var vid = slide.querySelector('video');
                    if (vid) vid.pause();
                }
            });
        }, 5000);
    }

    startSlideshow('heroMediaLeftInner', 'left');
    startSlideshow('heroMediaRightInner', 'right');

    // ================================================================
    // Welcome heading pulse — fades out/in every 5 seconds in sync
    // with the image slideshow.
    // ================================================================
    (function startHeadingPulse() {
        var heading = document.querySelector('.welcome-heading');
        if (!heading) return;

        setInterval(function () {
            heading.classList.add('welcome-heading-cycle');
            setTimeout(function () {
                heading.classList.remove('welcome-heading-cycle');
            }, 900);
        }, 5000);
    })();

    // ================================================================
    // Manage-slides modal (add / delete slides for a slot)
    // ================================================================
    var overlay = document.getElementById('mediaModalOverlay');
    var fileInput = document.getElementById('mediaFileInput');
    var errorEl = document.getElementById('mediaModalError');
    var cancelBtn = document.getElementById('btnCancelMedia');
    var saveBtn = document.getElementById('btnSaveMedia');
    var managerEl = document.getElementById('welcomeSlideManager');
    var currentSlot = null;

    function getAntiForgeryToken() {
        var el = document.querySelector('input[name="__RequestVerificationToken"]');
        return el ? el.value : '';
    }

    async function openModal(slot) {
        currentSlot = slot;
        fileInput.value = '';
        errorEl.textContent = '';
        overlay.classList.add('open');
        await loadSlideManager(slot);
    }
    function closeModal() {
        overlay.classList.remove('open');
        currentSlot = null;
    }

    async function loadSlideManager(slot) {
        managerEl.innerHTML = '<p class="welcome-modal-text">Loading…</p>';
        try {
            var response = await fetch('/Welcome/MediaList?slot=' + encodeURIComponent(slot));
            if (!response.ok) {
                managerEl.innerHTML = '<p class="welcome-modal-text">Could not load slides.</p>';
                return;
            }
            var items = await response.json();
            renderSlideManager(items);
        } catch (err) {
            managerEl.innerHTML = '<p class="welcome-modal-text">Network error.</p>';
        }
    }

    function renderSlideManager(items) {
        if (!items.length) {
            managerEl.innerHTML = '<p class="welcome-modal-text">No slides yet — add your first one below.</p>';
            return;
        }
        managerEl.innerHTML = '';
        items.forEach(function (item) {
            var row = document.createElement('div');
            row.className = 'welcome-slide-row';
            row.dataset.id = item.id;

            var thumbHtml = item.mediaType === 'video'
                ? '<video src="' + item.filePath + '" muted></video>'
                : '<img src="' + item.filePath + '" alt="" />';

            row.innerHTML =
                '<div class="welcome-slide-thumb">' + thumbHtml + '</div>' +
                '<span class="welcome-slide-type">' + (item.mediaType === 'video' ? 'Video' : 'Image') + '</span>' +
                '<button type="button" class="welcome-slide-delete" title="Delete this slide" aria-label="Delete this slide">&times;</button>';

            row.querySelector('.welcome-slide-delete').addEventListener('click', function () {
                deleteSlide(item.id, row);
            });

            managerEl.appendChild(row);
        });
    }

    async function deleteSlide(id, rowEl) {
        rowEl.classList.add('is-deleting');
        try {
            var formData = new FormData();
            formData.append('id', id);
            formData.append('__RequestVerificationToken', getAntiForgeryToken());

            var response = await fetch('/Welcome/DeleteMedia', { method: 'POST', body: formData });
            if (!response.ok) {
                rowEl.classList.remove('is-deleting');
                return;
            }

            rowEl.remove();

            var container = document.getElementById(slotContainerId(currentSlot));
            var liveSlide = container.querySelector('.hero-slide[data-id="' + id + '"]');
            var wasActive = liveSlide && liveSlide.classList.contains('active');
            if (liveSlide) liveSlide.remove();

            var remaining = container.querySelectorAll('.hero-slide');
            if (remaining.length === 0) {
                var placeholder = document.getElementById(slotPlaceholderId(currentSlot));
                if (!placeholder) {
                    container.innerHTML =
                        '<div class="hero-media-placeholder" id="' + slotPlaceholderId(currentSlot) + '">' +
                        '<svg viewBox="0 0 24 24"><rect x="3" y="7" width="18" height="14" rx="2" /><path d="M8 7V5a4 4 0 0 1 8 0v2" /></svg>' +
                        '<span>Add a photo or video</span>' +
                        '</div>';
                }
            } else if (wasActive) {
                remaining[0].classList.add('active');
                var v = remaining[0].querySelector('video');
                if (v) { v.currentTime = 0; claimSound(currentSlot, v); }
            }

            if (!managerEl.querySelector('.welcome-slide-row')) {
                managerEl.innerHTML = '<p class="welcome-modal-text">No slides yet — add your first one below.</p>';
            }
        } catch (err) {
            rowEl.classList.remove('is-deleting');
        }
    }

    document.querySelectorAll('.hero-media-edit-btn').forEach(function (btn) {
        btn.addEventListener('click', function () {
            openModal(btn.dataset.slot);
        });
    });

    cancelBtn.addEventListener('click', closeModal);
    overlay.addEventListener('click', function (e) {
        if (e.target === overlay) closeModal();
    });

    saveBtn.addEventListener('click', async function () {
        errorEl.textContent = '';

        if (!fileInput.files[0]) {
            errorEl.textContent = 'Please choose a photo or video first.';
            return;
        }

        var formData = new FormData();
        formData.append('slot', currentSlot);
        formData.append('file', fileInput.files[0]);
        formData.append('__RequestVerificationToken', getAntiForgeryToken());

        saveBtn.disabled = true;
        saveBtn.textContent = 'Uploading…';

        try {
            var response = await fetch('/Welcome/UploadMedia', { method: 'POST', body: formData });

            if (!response.ok) {
                var err = await response.json().catch(function () { return { message: 'Upload failed.' }; });
                errorEl.textContent = err.message || 'Upload failed.';
                return;
            }

            var result = await response.json();

            var container = document.getElementById(slotContainerId(currentSlot));
            var placeholder = document.getElementById(slotPlaceholderId(currentSlot));
            var isFirstSlide = !!placeholder;
            if (placeholder) placeholder.remove();

            var slideDiv = document.createElement('div');
            slideDiv.className = 'hero-slide' + (isFirstSlide ? ' active' : '');
            slideDiv.dataset.id = result.id;
            slideDiv.innerHTML = result.mediaType === 'video'
                ? '<video src="' + result.path + '" muted loop playsinline preload="metadata"></video>'
                : '<img src="' + result.path + '" alt="" />';
            container.appendChild(slideDiv);

            if (isFirstSlide) {
                var newVideo = slideDiv.querySelector('video');
                if (newVideo) claimSound(currentSlot, newVideo);
            }

            await loadSlideManager(currentSlot);
            fileInput.value = '';
        } catch (err) {
            errorEl.textContent = 'Network error. Please try again.';
        } finally {
            saveBtn.disabled = false;
            saveBtn.textContent = '+ Add';
        }
    });
});