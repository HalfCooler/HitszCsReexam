window.renderQuestionMath = function () {
    const list = document.querySelector('.question-list');
    if (!list || typeof renderMathInElement !== 'function') return;

    renderMathInElement(list, {
        delimiters: [
            { left: '$$', right: '$$', display: true },
            { left: '\\[', right: '\\]', display: true },
            { left: '$', right: '$', display: false },
            { left: '\\(', right: '\\)', display: false }
        ],
        throwOnError: false
    });
};
