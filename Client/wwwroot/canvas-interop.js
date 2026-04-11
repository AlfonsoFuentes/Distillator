// wwwroot/canvas-interop.js

window.canvasInterop = {
    // Función para bloquear el scroll del navegador en una zona específica
    preventScroll: function (element) {
        if (!element) return;

        element.addEventListener('wheel', function (e) {
            // Evita el scroll vertical/horizontal estándar de la página
            e.preventDefault();

            // Evita el Zoom nativo del navegador (Ctrl + Rueda o Pellizco en Trackpad)
            if (e.ctrlKey) {
                e.preventDefault();
            }
        }, { passive: false }); // CRÍTICO: passive false permite que el preventDefault funcione
    },

    // La función que ya tenías para medir el offset (la incluyo aquí por si no la tenías separada)
    getCanvasOffset: function (element) {
        if (!element) return { x: 0, y: 0 };
        var rect = element.getBoundingClientRect();
        return {
            x: rect.left,
            y: rect.top
        };
    },
    getCanvasDimensions: function (element) {
        if (!element) return { width: 800, height: 600 };
        var rect = element.getBoundingClientRect();
        return {
            width: rect.width,
            height: rect.height
        };
    }
};