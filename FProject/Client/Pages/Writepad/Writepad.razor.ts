import * as _ from "/lib/lodash-es/lodash.js";

let html: HTMLElement;
let pad: HTMLElement;
let panel: HTMLElement;
let canvas: HTMLCanvasElement;
let context: CanvasRenderingContext2D;

let componentRef;
let timeStampOrigin: number;
let writepad: Writepad;
let isSaving: boolean;
let isMiddleOfDrawing: boolean;
let saveInQueue: boolean;
let lastSavedDrawingNumber: number;
let undoStack: Point[][];
let deletedDrawings: DeletedDrawing[];
let num: number;
let lastPoint: Point;
let pointerX: number;
let pointerY: number;
let prevPointerX: number;
let prevPointerY: number;
let offsetX: number;
let offsetY: number;
let lastMoveTime: number;
let mode: Mode;
let defaultMode: Mode;
let pointerId: number;
let padRatio: number;
let horizontalOffset: number;
let verticalOffset: number;

export async function init(compRef, ratio: number, origin: number, writepadCompressedJson: string): Promise<void> {
    timeStampOrigin = origin;
    isSaving = false;
    isMiddleOfDrawing = false;
    saveInQueue = false;
    lastSavedDrawingNumber = -1;
    undoStack = [];
    deletedDrawings = [];
    num = 0;
    offsetX = 0;
    offsetY = 0;
    lastMoveTime = undefined;
    mode = Mode.Non;
    defaultMode = Mode.Non;

    componentRef = compRef;
    padRatio = ratio;
    let writepadReceived: Writepad;
    if (writepadCompressedJson) {
        writepadReceived = JSON.parse(await Decompress(writepadCompressedJson));
    } else {
        writepadReceived = JSON.parse(await Decompress(document.getElementById("data").innerHTML.slice(8)));
    }
    writepad = {
        LastModified: writepadReceived.LastModified,
        PointerType: writepadReceived.PointerType,
        Status: writepadReceived.Status,
        Type: writepadReceived.Type,
        Text: {
            Content: writepadReceived.Text?.Content
        },
        Points: writepadReceived.Points ?? []
    };
    if (writepad.Points.length != 0) {
        lastSavedDrawingNumber = _.last(writepad.Points).Number;
        num = lastSavedDrawingNumber + 1;
    }

    html = document.querySelector("html");
    pad = document.querySelector(".pad");
    panel = document.querySelector(".panel-container");
    canvas = <HTMLCanvasElement>document.getElementById("writepad");
    context = canvas.getContext("2d");
    canvas.width = 0;
    canvas.height = 0;

    redraw();

    window.addEventListener("resize", redraw);
    document.addEventListener("scroll", onScroll);

    canvas.addEventListener("pointerdown", onPointerDown);
    canvas.addEventListener("pointermove", onPointerMove);
    canvas.addEventListener("pointerup", onPointerUp);
    canvas.addEventListener("pointercancel", onPointerUp);
    canvas.addEventListener("pointerleave", onPointerUp);

    let writepadElement = document.querySelector(".writepad");
    writepadElement.addEventListener("contextmenu", e => e.preventDefault());

    updateDotNetUndoRedo();
}

export function pauseVideo() {
    (<HTMLVideoElement>document.getElementById("video"))?.pause();
}

function onScroll(event: Event) {
    updateOffsets();
}

function updateCanvasSize() {
    //let oldWidth = canvas.width;
    //let oldHeight = canvas.height;
    //if (panel.classList.contains("panel-collapsed")) {
    canvas.width = Math.trunc(Math.min(window.innerWidth, html.getBoundingClientRect().width) - panel.getBoundingClientRect().width);
    canvas.height = Math.trunc(Math.min(window.innerHeight, html.getBoundingClientRect().height));
    //} else {
    //    canvas.width = Math.round(window.innerWidth * padRatio);
    //    canvas.height = Math.round(window.innerHeight);
    //}
    //if (oldWidth != 0) {
    //    offsetX += canvas.width - oldWidth;
    //}
    //if (oldHeight != 0) {
    //    offsetY += canvas.height - oldHeight;
    //}
}

function updateOffsets() {
    horizontalOffset = canvas.getBoundingClientRect().left;
    verticalOffset = canvas.getBoundingClientRect().top;
}

export function isSaveRequired() {
    let endingIndex = _.findLastIndex(writepad.Points, (p: Point) => p.Type == PointType.Ending && p.Number > lastSavedDrawingNumber);
    if (endingIndex == -1 && !isSaving) {
        return false;
    }
    return true;
}

export async function save(): Promise<void> {
    if (isSaving) {
        return;
    } else if (isMiddleOfDrawing) {
        saveInQueue = true;
        return;
    }

    let startingIndex = _.findLastIndex(writepad.Points, (p: Point) => p.Number <= lastSavedDrawingNumber) + 1;
    let endingIndex = _.findLastIndex(writepad.Points, (p: Point) => p.Type == PointType.Ending && p.Number > lastSavedDrawingNumber);
    if (!isSaveRequired()) {
        return;
    }

    try {
        //console.log("Start!" + new Date());
        isSaving = true;

        let validDeletedDrawings = deletedDrawings.filter(d => d.StartingNumber <= lastSavedDrawingNumber);

        let newDrawings = writepad.Points.slice(startingIndex, endingIndex + 1);

        //console.log("Middle1!" + new Date());
        let savePointsDTO: SavePointsDTO = {
            LastModified: writepad.LastModified,
            NewPoints: newDrawings,
            DeletedDrawings: validDeletedDrawings
        };
        let rawData = JSON.stringify(savePointsDTO);
        let data = await Compress(rawData);
        //console.log(data.length / rawData.length);
        let response: SaveResponseDTO = await componentRef.invokeMethodAsync("Save", data);
        //let response: SaveResponseDTO = await componentRef.invokeMethodAsync("Save", writepad.LastModified, newDrawings, validDeletedDrawings);
        switch (response.statusCode) {
            case StatusCode.ClientErrorNotFound:
            case StatusCode.ClientErrorBadRequest:
                break;
            case StatusCode.SuccessOK:
                writepad.LastModified = response.lastModified;
                lastSavedDrawingNumber = _.last(newDrawings).Number;
                break;
            default:
                break;
        }
        //console.log("End!" + new Date());
    } finally {
        isSaving = false;
        await componentRef.invokeMethodAsync("ReleaseSaveToken");
    }
}

interface SavePointsDTO {
    LastModified: Date,
    NewPoints: Point[],
    DeletedDrawings: DeletedDrawing[]
}

interface SaveResponseDTO {
    statusCode: StatusCode,
    lastModified: Date
}

async function updateDotNetUndoRedo() {
    let undo = writepad.Points.length != 0;
    let redo = undoStack.length != 0;

    await componentRef.invokeMethodAsync("UndoRedoUpdator", undo, redo);
}

export function undo() {
    if (_.last(writepad.Points).Type != PointType.Ending) {
        throw new Error("Drawing is in progress!") // TODO: check if the situation is possible. if yes, you should handle it!
    }
    let lastStartingIndex = _.findLastIndex(writepad.Points, (p: Point) => p.Type == PointType.Starting);
    let deletedPoints = writepad.Points.splice(lastStartingIndex);
    undoStack.push(deletedPoints);
    deletedDrawings.push({
        StartingNumber: deletedPoints[0].Number,
        EndingNumber: _.last(deletedPoints).Number
    });
    redraw();

    updateDotNetUndoRedo();
}

export function redo() {
    let lastPoint = _.last(writepad.Points);
    if (lastPoint && lastPoint.Type != PointType.Ending) {
        throw new Error("Drawing is in progress!")
    }
    if (undoStack.length == 0) {
        return;
    }
    let deletedPoints = undoStack.pop();
    deletedPoints.forEach((point) => writepad.Points.push(point));
    deletedDrawings.pop();
    redraw();

    updateDotNetUndoRedo();
}

function toFixedNumber(number: number, digits: number, base = 10): number {
    let power = Math.pow(base, digits);
    return Math.round(number * power) / power;
}

interface Writepad {
    LastModified: Date;
    readonly PointerType: PointerType;
    readonly Status: WritepadStatus;
    readonly Type: WritepadType;
    readonly Text: WritepadText;
    readonly Points: Point[];
}

const enum WritepadStatus {
    Editing,
    WaitForAcceptance,
    Accepted
}

interface WritepadText {
    readonly Content: string;
}

interface Point {
    readonly Number: number;
    Type: PointType;
    readonly TimeStamp: number;
    readonly X: number;
    readonly Y: number;
    readonly Width: number;
    readonly Height: number;
    readonly Pressure: number;
    readonly TangentialPressure: number;
    readonly TiltX: number;
    readonly TiltY: number;
    readonly Twist: number;
}

interface DeletedDrawing {
    StartingNumber: number,
    EndingNumber: number
}

const enum WritepadType {
    Text,
    WordGroup,
    Sign
}

const enum PointerType {
    Mouse,
    Touchpad,
    Pen,
    Touch,
    TouchPen
}

const enum PointType {
    Middle,
    Starting,
    Ending
}

const enum Mode {
    Non,
    Draw,
    Erase,
    Move
}

export function changeDefaultMode(mode: Mode) {
    defaultMode = mode;
}

function toScreenX(realX: number): number {
    return realX + offsetX;
}

function toScreenY(realY: number): number {
    return realY + offsetY;
}

export function redraw(): void {
    updateCanvasSize();
    updateOffsets();
    let minX = - canvas.width;
    let maxX = 2 * canvas.width;
    let minY = - canvas.height;
    let maxY = 2 * canvas.height;
    let isInside: boolean;
    let lastP: Point;
    let lastX: number;
    let lastY: number;

    context.clearRect(0, 0, canvas.width, canvas.height);
    for (let p of writepad.Points) {
        let screenX = toScreenX(p.X);
        let screenY = toScreenY(p.Y);

        if (!isInside &&
            screenX > minX && screenX < maxX &&
            screenY > minY && screenY < maxY) {
            isInside = true;
        }

        switch (p.Type) {
            case PointType.Starting:
                context.beginPath();
                context.moveTo(screenX, screenY);
                break;
            case PointType.Middle:
                if (lastX != screenX || lastY != screenY) {
                    context.lineTo(screenX, screenY);
                }
                break;
            case PointType.Ending:
                if (isInside) {
                    let isDot = lastP.Type == PointType.Starting;
                    if (isDot) {
                        context.fillRect(screenX, screenY, 1, 1);
                    }
                    else {
                        if (lastX != screenX || lastY != screenY) {
                            context.lineTo(screenX, screenY);
                        }
                        context.stroke();
                    }
                    isInside = false;
                }
                break;
        }

        lastX = screenX;
        lastY = screenY;
        lastP = p;
    }
}

function toRealX(screenX: number): number {
    return screenX - offsetX;
}

function toRealY(screenY: number): number {
    return screenY - offsetY;
}

function createPoint(event: PointerEvent, type: PointType, x: number, y: number, num: number): Point {
    return {
        Number: num,
        Type: type,
        TimeStamp: toFixedNumber(event.timeStamp, 3) + timeStampOrigin,
        X: toFixedNumber(x, 5),
        Y: toFixedNumber(y, 5),
        Width: toFixedNumber(event.width, 3),
        Height: toFixedNumber(event.height, 3),
        Pressure: toFixedNumber(event.pressure, 5),
        TangentialPressure: toFixedNumber(event.tangentialPressure, 5),
        TiltX: toFixedNumber(event.tiltX, 3),
        TiltY: toFixedNumber(event.tiltY, 3),
        Twist: Math.round(event.twist)
    };
}

function addToDrawings(point: Point): void {
    writepad.Points.push(point);
    num++;
}

function addStartingPoint(): void {
    if (undoStack.length != 0) {
        undoStack.length = 0;
        updateDotNetUndoRedo();
    }
    addToDrawings(lastPoint);
    if (writepad.Points.length == 1) {
        updateDotNetUndoRedo();
    }
}

function detectPointerType(type: string): PointerType {
    switch (type) {
        case "mouse":
            if (writepad.PointerType == PointerType.Mouse || writepad.PointerType == PointerType.Touchpad)
                return writepad.PointerType;
            else return PointerType.Mouse;
        case "touch":
            if (writepad.PointerType == PointerType.Touch || writepad.PointerType == PointerType.TouchPen)
                return writepad.PointerType;
            else return PointerType.Touch;
        case "pen":
            return PointerType.Pen;
        default:
            return PointerType.Mouse;
    }
}
// TODO: Check touchpad and mouse at the beginning https://stackoverflow.com/questions/10744645/detect-touchpad-vs-mouse-in-javascript/62415754#62415754
// TODO: Distinguish Touch and TouchPen

function detectMode(event: PointerEvent): Mode {
    if (defaultMode != Mode.Non) {
        return defaultMode;
    }

    if (event.button == 0 && event.buttons == 1 //Left Mouse, Touch Contact, Pen contact
        && detectPointerType(event.pointerType) == writepad.PointerType) {
        if (event.isPrimary) {
            return Mode.Draw;
        }
        else {
            return Mode.Move;
        }
    } else if (mode == Mode.Non) {
        return Mode.Move;
    }
}

async function draw(startX, startY, endX, endY, isDot: boolean = false) {
    if (isDot && startX == endX && startY == endY) {
        context.fillRect(endX, endY, 1, 1);
    }
    else {
        context.beginPath();
        context.moveTo(startX, startY);
        context.lineTo(endX, endY);
        context.stroke();
    }
}

function onPointerDown(event: PointerEvent) {
    event.preventDefault();

    if (isMiddleOfDrawing) {
        return;
    }

    mode = detectMode(event);

    pointerId = event.pointerId;

    pointerX = prevPointerX = event.clientX - horizontalOffset;
    pointerY = prevPointerY = event.clientY - verticalOffset;

    switch (mode) {
        case Mode.Draw:
            lastPoint = createPoint(event, PointType.Starting, toRealX(pointerX), toRealY(pointerY), num);
            break;
        case Mode.Move:
            lastMoveTime = event.timeStamp;
            break;
    }
}

function onPointerMove(event: PointerEvent) {
    event.preventDefault();

    if (pointerId != event.pointerId)
        return;

    isMiddleOfDrawing = true;

    pointerX = event.clientX - horizontalOffset;
    pointerY = event.clientY - verticalOffset;

    switch (mode) {
        case Mode.Draw:
            if (lastPoint.Type == PointType.Starting) {
                addStartingPoint();
            }

            if (lastPoint.TimeStamp != event.timeStamp) {
                const realX = toRealX(pointerX);
                const realY = toRealY(pointerY);
                if (lastPoint.X != realX || lastPoint.Y != realY) {
                    draw(prevPointerX, prevPointerY, pointerX, pointerY);
                }
                lastPoint = createPoint(event, PointType.Middle, realX, realY, num);
                addToDrawings(lastPoint);
            }
            break;
        case Mode.Move:
            offsetX += pointerX - prevPointerX;
            offsetY += pointerY - prevPointerY;
            if (event.timeStamp - lastMoveTime > 200) {
                lastMoveTime = event.timeStamp;
                redraw();
            }
            break;
    }

    prevPointerX = pointerX;
    prevPointerY = pointerY;
}

function onPointerUp(event: PointerEvent) {
    event.preventDefault();

    if (pointerId != event.pointerId)
        return;

    isMiddleOfDrawing = true;

    pointerX = event.clientX - horizontalOffset;
    pointerY = event.clientY - verticalOffset;

    switch (mode) {
        case Mode.Draw:
            if (lastPoint.Type == PointType.Starting) {
                addStartingPoint();
            }

            const realX = toRealX(pointerX);
            const realY = toRealY(pointerY);
            if (lastPoint.Type == PointType.Starting
                || lastPoint.TimeStamp != event.timeStamp
                || lastPoint.X != realX
                || lastPoint.Y != realY) {
                draw(prevPointerX, prevPointerY, pointerX, pointerY, lastPoint.Type == PointType.Starting);
                lastPoint = createPoint(event, PointType.Ending, realX, realY, num);
                addToDrawings(lastPoint);
            } else {
                lastPoint.Type = PointType.Ending;
            }
            break;
        case Mode.Move:
            redraw();
            lastMoveTime = undefined;
            break;
    }

    mode = defaultMode;
    pointerId = undefined;
    isMiddleOfDrawing = false;

    if (saveInQueue) {
        saveInQueue = false;
        save();
    }
}
