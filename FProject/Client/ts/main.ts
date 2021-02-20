async function ImportGlobal(name: string, uri: string) {
    const module = await import(uri);
    window[name] = module;
    return module;
}

function Compress(content: string): string {
    return LZString.compressToBase64(content);
}

function Decompress(content: string): string {
    return LZString.decompressFromBase64(content);
}
