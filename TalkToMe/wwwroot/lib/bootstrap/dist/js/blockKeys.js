window.blockInvalidKeys = function (selector, allowedPattern) {
    const input = document.querySelector(selector);
    if (!input) return;
    const regex = new RegExp(allowedPattern);
    input.addEventListener("keydown", function (e) {
        if (!regex.test(e.key) &&
            e.key !== "Backspace" &&
            e.key !== "Tab" &&
            e.key !== "ArrowLeft" &&
            e.key !== "ArrowRight" &&
            e.key !== "Delete" &&
            e.key !== "Enter") {
            e.preventDefault();
        }
    });
}