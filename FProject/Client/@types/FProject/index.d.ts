import { DotNetReferenceType } from "../../Ts/baseComponent";

declare global {
    interface Window {
        FProject: FProject;
    }
}

interface FProject {
    VERSION?: string;
    UA?: UAParser.IResult;
    AddDoneEndHandler?(component: DotNetReferenceType, element: HTMLElement): void;
    CompressAsync?(content: string): Promise<string>;
    DecompressAsync?(content: string): Promise<string>;
    ImportGlobal?(name: string, uri: string): Promise<any>;
    GetParsedUA?(): UAParser.IResult;
    CheckBrowser?(): void;
    IsNullOrWhiteSpace?(input): boolean;
    UnsupportedBrowser?: boolean;
    GetUnsupportedBrowser?(): boolean;
}
