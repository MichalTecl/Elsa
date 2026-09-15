namespace DocGen
{
    internal static class DocumentHighlighting
    {
        // No external assets: the generated document remains self-contained.
        internal const string Script = @"
(function () {
    var hovered = null;
    var focused = null;
    var active = null;
    var marked = [];

    function sourceFor(node) {
        if (!node || !node.closest) return null;
        return node.closest('[data-highlight-targets]');
    }

    function refresh() {
        var source = hovered || focused;
        if (source === active) return;
        marked.forEach(function (element) {
            element.classList.remove('linked-highlight', 'trend-highlight');
        });
        marked = [];
        active = source;
        if (!source) return;
        var section = source.closest('section');
        if (!section) return;
        var keys = source.getAttribute('data-highlight-targets').split(' ');
        var isTrend = source.getAttribute('data-highlight-kind') === 'trend';
        var attribute = isTrend ? 'data-trend-key' : 'data-link-key';
        var className = isTrend ? 'trend-highlight' : 'linked-highlight';
        section.querySelectorAll('[' + attribute + ']').forEach(function (element) {
            if (keys.indexOf(element.getAttribute(attribute)) === -1) return;
            element.classList.add(className);
            marked.push(element);
        });
        source.classList.add(className);
        marked.push(source);
    }

    document.addEventListener('pointerover', function (event) {
        hovered = sourceFor(event.target);
        refresh();
    });
    document.addEventListener('pointerout', function (event) {
        hovered = sourceFor(event.relatedTarget);
        refresh();
    });
    document.addEventListener('focusin', function (event) {
        focused = sourceFor(event.target);
        refresh();
    });
    document.addEventListener('focusout', function (event) {
        focused = sourceFor(event.relatedTarget);
        refresh();
    });
    window.addEventListener('blur', function () {
        hovered = null;
        focused = null;
        refresh();
    });
})();
";
    }
}
