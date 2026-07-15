const attachedEditors = new WeakSet();

export function attach(editor) {
    if (!editor || attachedEditors.has(editor)) {
        return;
    }

    editor.addEventListener("keydown", event => {
        const popup = editor.closest(".formula-editor")?.querySelector(".suggestions");
        const items = popup ? Array.from(popup.querySelectorAll(".suggestion-item")) : [];
        if (items.length === 0) {
            return;
        }

        let activeIndex = items.findIndex(item => item.classList.contains("active"));
        if (activeIndex < 0) {
            activeIndex = 0;
        }

        if (event.key === "ArrowDown" || event.key === "ArrowUp") {
            event.preventDefault();
            activeIndex = event.key === "ArrowDown"
                ? (activeIndex + 1) % items.length
                : (activeIndex - 1 + items.length) % items.length;
            setActiveItem(items, activeIndex);
            return;
        }

        if (event.key === "ArrowRight" || event.key === "Enter" || event.key === "Tab") {
            event.preventDefault();
            items[activeIndex].click();
            return;
        }

        if (event.key === "Escape") {
            popup.hidden = true;
        }
    });

    attachedEditors.add(editor);
}

export function getCaret(editor) {
    return editor?.selectionStart ?? 0;
}

export function replaceRange(editor, start, end, replacement) {
    if (!editor) {
        return "";
    }

    const safeStart = Math.max(0, Math.min(start, editor.value.length));
    const safeEnd = Math.max(safeStart, Math.min(end, editor.value.length));
    editor.setRangeText(replacement, safeStart, safeEnd, "end");
    editor.focus();
    return editor.value;
}

export function positionSuggestions(editor) {
    const popup = editor?.closest(".formula-editor")?.querySelector(".suggestions");
    if (!editor || !popup) {
        return;
    }

    popup.hidden = false;
    const caretPosition = getCaretCoordinates(editor);
    const maxLeft = Math.max(8, editor.clientWidth - popup.offsetWidth + editor.offsetLeft);
    popup.style.left = `${Math.min(caretPosition.left, maxLeft)}px`;
    popup.style.top = `${caretPosition.top}px`;
}

function setActiveItem(items, activeIndex) {
    items.forEach((item, index) => item.classList.toggle("active", index === activeIndex));
    items[activeIndex].scrollIntoView({ block: "nearest" });
}

function getCaretCoordinates(editor) {
    const style = window.getComputedStyle(editor);
    const mirror = document.createElement("div");
    const copiedProperties = [
        "boxSizing", "width", "height", "overflowX", "overflowY",
        "borderTopWidth", "borderRightWidth", "borderBottomWidth", "borderLeftWidth",
        "paddingTop", "paddingRight", "paddingBottom", "paddingLeft",
        "fontStyle", "fontVariant", "fontWeight", "fontStretch", "fontSize",
        "fontFamily", "lineHeight", "textAlign", "textTransform", "textIndent",
        "letterSpacing", "wordSpacing", "tabSize"
    ];

    mirror.style.position = "absolute";
    mirror.style.visibility = "hidden";
    mirror.style.whiteSpace = "pre-wrap";
    mirror.style.wordWrap = "break-word";
    copiedProperties.forEach(property => mirror.style[property] = style[property]);

    mirror.textContent = editor.value.substring(0, editor.selectionStart);
    const marker = document.createElement("span");
    marker.textContent = editor.value.substring(editor.selectionStart) || ".";
    mirror.appendChild(marker);
    document.body.appendChild(mirror);

    const left = editor.offsetLeft + marker.offsetLeft - editor.scrollLeft;
    const top = editor.offsetTop + marker.offsetTop - editor.scrollTop + parseFloat(style.lineHeight || style.fontSize);
    mirror.remove();

    return { left, top };
}
