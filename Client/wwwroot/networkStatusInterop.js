window.distillatorNetworkStatus = {
    dotNetReference: null,
    onlineHandler: null,
    offlineHandler: null,
    pollHandle: null,
    wasOffline: false,

    registerOnlineHandler: function (dotNetReference) {
        this.unregisterOnlineHandler();
        this.dotNetReference = dotNetReference;
        this.wasOffline = !navigator.onLine;
        this.onlineHandler = function () {
            if (!window.distillatorNetworkStatus.dotNetReference) return;

            window.distillatorNetworkStatus.dotNetReference.invokeMethodAsync('OnBrowserOnlineAsync');
            window.distillatorNetworkStatus.wasOffline = false;
        };
        this.offlineHandler = function () {
            window.distillatorNetworkStatus.wasOffline = true;
        };

        window.addEventListener('online', this.onlineHandler);
        window.addEventListener('offline', this.offlineHandler);
        this.pollHandle = window.setInterval(function () {
            var status = window.distillatorNetworkStatus;
            if (!navigator.onLine) {
                status.wasOffline = true;
                return;
            }

            if (!status.wasOffline || !status.dotNetReference) return;

            status.wasOffline = false;
            status.dotNetReference.invokeMethodAsync('OnBrowserOnlineAsync');
        }, 2000);
    },

    unregisterOnlineHandler: function () {
        if (this.onlineHandler) {
            window.removeEventListener('online', this.onlineHandler);
        }

        if (this.offlineHandler) {
            window.removeEventListener('offline', this.offlineHandler);
        }

        if (this.pollHandle) {
            window.clearInterval(this.pollHandle);
        }

        this.onlineHandler = null;
        this.offlineHandler = null;
        this.pollHandle = null;
        this.dotNetReference = null;
        this.wasOffline = false;
    }
};
