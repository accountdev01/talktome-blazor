window.blazorJsInterop = {
    closeDetailsOnOutsideClick: (detailsElement) => {
        if (!detailsElement) return;

        document.addEventListener('click', (event) => {
            if (!detailsElement.contains(event.target)) {
                detailsElement.removeAttribute('open');
            }
        }, true);
    }
};
