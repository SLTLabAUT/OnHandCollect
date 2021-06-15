async function ImportGlobal(name: string, uri: string) {
    const module = await import(uri);
    window[name] = module;
    return module;
}

async function Compress(content: string): Promise<string> {
    return LZString.compressToBase64(content);
}

async function Decompress(content: string): Promise<string> {
    return LZString.decompressFromBase64(content);
}
