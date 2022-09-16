window.FProject.IsNullOrWhiteSpace = function (input) {
    return !input || !input.trim();
}

window.FProject.ImportGlobal = async function (name, uri) {
    let url: URL;
    if (uri.startsWith("/")) {
        url = new URL(window.location.origin + uri);
    }
    else {
        url = new URL(window.location.origin + window.location.pathname + "/" + uri);
    }
    if (!url.searchParams.has("version")) {
        url.searchParams.set("version", window.FProject.VERSION);
    }
    const module = await import(url.pathname + url.search);
    window[name] = module;
    return module;
}

window.FProject.CompressAsync = function (content) {
    return new Promise<string>((resolve, _) => resolve(content))
        .then(content => {
            return LZString.compressToBase64(content);
        });
}

window.FProject.DecompressAsync = function (content) {
    return new Promise<string>((resolve, _) => resolve(content))
        .then(content => {
            return LZString.decompressFromBase64(content);
        });
}

window.FProject.AddDoneEndHandler = function (component, element) {
    element.addEventListener("animationend", async _ => {
        await component.invokeMethodAsync("DoneEndHandler");
    });
};

window.FProject.GetParsedUA = function () {
    if (!window.FProject.UA) {
        let parser = new UAParser();
        window.FProject.UA = parser.getResult();
    }
    return window.FProject.UA;
};

window.FProject.UnsupportedBrowser = false;
let USBDialog = document.getElementById("unsupported-browser");
USBDialog.addEventListener("animationend", function () {
    if (window.FProject.UnsupportedBrowser) {
        USBDialog.classList.remove("ms-motion-fadeIn");
    } else {
        USBDialog.style.display = "none";
        USBDialog.classList.remove("ms-motion-fadeOut");
    }
})
document.querySelectorAll("#unsupported-browser .ms-Dialog-button").forEach(e => {
    e.addEventListener("click", function () {
        window.FProject.UnsupportedBrowser = false;
        USBDialog.classList.add("ms-motion-fadeOut");
    });
});
window.FProject.GetUnsupportedBrowser = function () {
    return window.FProject.UnsupportedBrowser;
}
window.FProject.CheckBrowser = function () {
    let ua = window.FProject.GetParsedUA();

    if (!ua || !ua.browser || window.FProject.IsNullOrWhiteSpace(ua.browser.name)) {
        return;
    }

    let showBrowserWarning = false;
    let browserWarningDescription = 'مرورگر شما جزو مرورگرهای تحت پشتیبانی سامانه نمی‌باشد.<br>لطفا مرورگر خود را به‌روز کنید.';

    switch (ua.browser.name) {
        case "IE":
        case "Opera Mini":
        case "UCBrowser":
        case "Baidu":
        case "Samsung Browser":
            showBrowserWarning = true;
            browserWarningDescription = "مرورگر شما جزو مرورگرهای تحت پشتیبانی سامانه نمی‌باشد.<br>لطفا از مرورگر دیگری استفاده کنید.";
            break;
    }

    if (!showBrowserWarning && !window.FProject.IsNullOrWhiteSpace(ua.browser.version)) {
        let version = ua.browser.version;
        let desired = null;

        switch (ua.browser.name) {
            case "Mobile Safari":
                desired = '13.0';
                break;
            case "Opera":
                desired = '44.0';
                break;
            case "Safari":
                desired = '13.0';
                break;
            case "Chrome":
                desired = '57.0';
                break;
            case "Firefox":
                desired = '64.0';
                break;
            case "Edge":
                desired = '16.0';
                break;
        }
        if (desired)
        {
            showBrowserWarning = compareVersions.compare(version, desired, '<');
        }
    }

    if (!showBrowserWarning && ua.engine && ua.engine.name == "WebKit" && ua.os && ua.os.name == "iOS" && !window.FProject.IsNullOrWhiteSpace(ua.os.version)) {
        let version = ua.os.version;
        let desired = '13.0';
        showBrowserWarning = compareVersions.compare(version, desired, '<');
    }

    if (showBrowserWarning) {
        USBDialog.querySelector(".ms-Dialog-innerContent").innerHTML = browserWarningDescription;
        window.FProject.UnsupportedBrowser = true;
        USBDialog.classList.add("ms-motion-fadeIn");
        USBDialog.style.display = "";
    }
}
window.FProject.CheckBrowser();
