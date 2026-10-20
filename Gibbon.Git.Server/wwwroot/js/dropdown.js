document.addEventListener('click', function (event) {
    document.querySelectorAll('details.dropdown[open]').forEach(function (dropdown) {
        if (!dropdown.contains(event.target) || event.target.closest('.dropdown-menu a')) {
            dropdown.removeAttribute('open');
        }
    });
});

document.addEventListener('keydown', function (event) {
    if (event.key === 'Escape') {
        document.querySelectorAll('details.dropdown[open]').forEach(function (dropdown) {
            dropdown.removeAttribute('open');
            dropdown.querySelector('summary').focus();
        });
    }
});
