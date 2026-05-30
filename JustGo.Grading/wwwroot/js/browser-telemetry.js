// Captures browser errors and unhandled promise rejections, forwarding them to
// the server as structured log entries so they appear in the Aspire dashboard.
(function () {
    'use strict';

    function sendError(payload) {
        navigator.sendBeacon('/telemetry/browser-error', JSON.stringify(payload));
    }

    window.addEventListener('error', function (event) {
        sendError({
            message: event.message,
            source: event.filename,
            line: event.lineno,
            column: event.colno,
            stack: event.error ? event.error.stack : null
        });
    });

    window.addEventListener('unhandledrejection', function (event) {
        const reason = event.reason;
        sendError({
            message: reason instanceof Error ? reason.message : String(reason),
            source: 'unhandledrejection',
            line: null,
            column: null,
            stack: reason instanceof Error ? reason.stack : null
        });
    });
})();
