window.FProject = {};

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
