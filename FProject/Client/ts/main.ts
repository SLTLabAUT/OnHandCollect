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

async function Compress(content: string): Promise<string> {
    return LZString.compressToBase64(content);
}

async function Decompress(content: string): Promise<string> {
    return LZString.decompressFromBase64(content);
}
