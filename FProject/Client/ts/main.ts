async function ImportGlobal(name: string, uri: string) {
    let url: URL;
    if (uri.startsWith("/")) {
        url = new URL(window.location.origin + uri);
    }
    else {
        url = new URL(window.location.origin + window.location.pathname + "/" + uri);
    }
    if (!url.searchParams.has("version")) {
        url.searchParams.set("version", window.VERSION);
    }
    const module = await import(url.pathname + url.search);
    window[name] = module;
    return module;
}

function CompressAsync(content: string): Promise<string> {
    return new Promise<string>((resolve, _) => resolve(content))
        .then(content => {
            return LZString.compressToBase64(content);
        });
}

function DecompressAsync(content: string): Promise<string> {
    return new Promise<string>((resolve, _) => resolve(content))
        .then(content => {
            return LZString.decompressFromBase64(content);
        });
}
