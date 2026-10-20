document.addEventListener('DOMContentLoaded', function () {
    initClipboard();
});

function initClipboard() {
    document.body.addEventListener('click', function (event) {
        const button = event.target.closest('[data-clipboard-text]');
        if (button) {
            const textToCopy = button.getAttribute('data-clipboard-text');
            navigator.clipboard.writeText(textToCopy)
                .then(() => {
                    button.classList.add('copied');
                    setTimeout(() => button.classList.remove('copied'), 1500);
                })
                .catch(err => {
                    console.error('Failed to copy!', err);
                });
        }
    });
}
