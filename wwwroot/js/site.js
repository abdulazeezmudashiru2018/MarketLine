document.addEventListener('DOMContentLoaded', function () {
    var hamburger = document.getElementById('mlHamburger');
    var navLinks = document.getElementById('mlNavLinks');

    if (!hamburger || !navLinks) return;

    hamburger.addEventListener('click', function () {
        var isOpen = navLinks.classList.toggle('open');
        hamburger.classList.toggle('open', isOpen);
        hamburger.setAttribute('aria-expanded', isOpen ? 'true' : 'false');
    });

    // Close the menu when a link is tapped (mobile)
    navLinks.querySelectorAll('a').forEach(function (link) {
        link.addEventListener('click', function () {
            navLinks.classList.remove('open');
            hamburger.classList.remove('open');
            hamburger.setAttribute('aria-expanded', 'false');
        });
    });

    // Close menu on outside click
    document.addEventListener('click', function (e) {
        if (!navLinks.contains(e.target) && !hamburger.contains(e.target)) {
            navLinks.classList.remove('open');
            hamburger.classList.remove('open');
            hamburger.setAttribute('aria-expanded', 'false');
        }
    });
});
