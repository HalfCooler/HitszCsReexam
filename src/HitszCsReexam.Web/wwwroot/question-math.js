window.renderQuestionMath = function () {
    if (typeof renderMathInElement !== 'function') return;

    document.querySelectorAll('.question-list, .practice-question, .exam-question, .report-page').forEach(list => renderMathInElement(list, {
        delimiters: [
            { left: '$$', right: '$$', display: true },
            { left: '\\[', right: '\\]', display: true },
            { left: '$', right: '$', display: false },
            { left: '\\(', right: '\\)', display: false }
        ],
        throwOnError: false
    }));
};
