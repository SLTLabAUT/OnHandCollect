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
let undoStack: Point[][];
let deletedDrawings: DrawingRange[];
let recoveredDrawings: DrawingRange[];
let selectedDrawings: DrawingRange[];
let num: number;
let count: number;
let lastPoint: Point;
let tempPoints: Point[];
let pointerX: number;
let pointerY: number;
let prevPointerX: number;
let prevPointerY: number;
let offsetX: number;
let offsetY: number;
let firstPointerTime: number;
let lastMoveTime: number;
let mode: Mode;
let defaultMode: Mode;
let pointerId: number;
let padRatio: number;
let horizontalOffset: number;
let verticalOffset: number;
let selectionBox: SelectionBox;

export async function init(compRef, ratio: number, origin: number, writepadCompressedJson: string): Promise<void> {
    timeStampOrigin = origin;
    isSaving = false;
    isMiddleOfDrawing = false;
    saveInQueue = false;
    undoStack = [];
    deletedDrawings = [];
    recoveredDrawings = [];
    selectedDrawings = [];
    count = 0;
    tempPoints = [];
    offsetX = 0;
    offsetY = 0;
    firstPointerTime = undefined;
    lastMoveTime = undefined;
    mode = Mode.Non;
    defaultMode = Mode.Non;
    selectionBox = undefined;

    componentRef = compRef;
    padRatio = ratio;
    let writepadReceived: Writepad;
    if (writepadCompressedJson) {
        writepadReceived = JSON.parse(await window.FProject.DecompressAsync(writepadCompressedJson));
    } else {
        writepadReceived = JSON.parse(await window.FProject.DecompressAsync(document.getElementById("data").innerHTML.slice(8)));
    }
    writepad = {
        LastModified: writepadReceived.LastModified,
        LastSavedDrawingNumber: writepadReceived.LastSavedDrawingNumber,
        PointerType: writepadReceived.PointerType,
        Status: writepadReceived.Status,
        Type: writepadReceived.Type,
        Text: {
            Content: writepadReceived.Text?.Content
        },
        Points: writepadReceived.Points ?? []
    };
    num = writepad.LastSavedDrawingNumber;

    html = document.querySelector("html");
    pad = document.querySelector(".pad");
    panel = document.querySelector(".panel-container");
    canvas = <HTMLCanvasElement>document.getElementById("writepad");
    context = canvas.getContext("2d");
    canvas.width = 0;
    canvas.height = 0;
    initContext();

    redraw();
    checkOrientation();

    window.addEventListener("resize", onResize);
    document.addEventListener("scroll", onScroll);
    document.addEventListener("keyup", onKeyUp);

    canvas.addEventListener("pointerdown", onPointerDown);
    canvas.addEventListener("pointermove", onPointerMove);
    canvas.addEventListener("pointerup", onPointerUp);
    canvas.addEventListener("pointercancel", onPointerUp);
    canvas.addEventListener("pointerleave", onPointerUp);

    let writepadElement = document.querySelector(".writepad");
    writepadElement.addEventListener("contextmenu", e => e.preventDefault());

    updateDotNetUndoRedo();
}

function initContext() {
    context.lineCap = "round";
    context.lineJoin = "round";
    context.lineWidth = 2;
    context.strokeStyle = "black";
    context.setLineDash([]);
}

function onResize() {
    redraw();
    checkOrientation();
}

function checkOrientation() {
    let tips = document.querySelector(".tips");
    let phoneRotate = document.querySelector(".phone-rotate");

    if (window.innerWidth >= 640 || window.innerWidth >= window.innerHeight) {
        if (tips.classList.contains("blur-animation")) {
            phoneRotate.classList.add("ms-motion-fadeOut");
            phoneRotate.classList.remove("ms-motion-fadeIn");
            tips.classList.add("unblur-animation");
            tips.classList.remove("blur-animation");
        }
    }
    else if (!phoneRotate.classList.contains("ms-motion-fadeIn")) {
        tips.classList.add("blur-animation");
        tips.classList.remove("unblur-animation");
        phoneRotate.classList.add("ms-motion-fadeIn");
        phoneRotate.classList.remove("ms-motion-fadeOut");
    }
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
    canvas.width = 0;
    canvas.height = 0;
    canvas.width = Math.trunc(pad.getBoundingClientRect().width);
    canvas.height = Math.trunc(pad.getBoundingClientRect().height);
    initContext();
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
    let endingIndex = _.findLastIndex(writepad.Points, (p: Point) => p.Type == PointType.Ending && p.Number > writepad.LastSavedDrawingNumber);
    let deletedIndex = _.findIndex(deletedDrawings, (d: DrawingRange) => d.StartingNumber < writepad.LastSavedDrawingNumber);
    let recoveredIndex = _.findIndex(recoveredDrawings, (d: DrawingRange) => d.StartingNumber < writepad.LastSavedDrawingNumber);
    if (endingIndex == -1 && deletedIndex == -1 && recoveredIndex == -1 && !isSaving) {
        return false;
    }
    return true;
}

export async function save(): Promise<boolean> {
    if (isSaving) {
        return true;
    } else if (isMiddleOfDrawing) {
        saveInQueue = true;
        return true;
    }

    if (!isSaveRequired()) {
        return true;
    }

    let succeed = true;
    try {
        //console.log("Start!" + new Date());
        isSaving = true;

        let validDeletedDrawings = deletedDrawings.filter(d => d.StartingNumber < writepad.LastSavedDrawingNumber);
        let validRecoveredDrawings = recoveredDrawings.filter(d => d.StartingNumber < writepad.LastSavedDrawingNumber);

        let startingIndex = _.findLastIndex(writepad.Points, (p: Point) => p.Number <= writepad.LastSavedDrawingNumber) + 1;
        let endingIndex = _.findLastIndex(writepad.Points, (p: Point) => p.Type == PointType.Ending && p.Number > writepad.LastSavedDrawingNumber);
        let newDrawings = writepad.Points.slice(startingIndex, endingIndex + 1);

        //console.log("Middle1!" + new Date());
        let savePointsDTO: SavePointsDTO = {
            LastModified: writepad.LastModified,
            NewPoints: newDrawings,
            DeletedDrawings: validDeletedDrawings,
            RecoveredDrawings: validRecoveredDrawings
        };
        let rawData = JSON.stringify(savePointsDTO);
        let data = await window.FProject.CompressAsync(rawData);
        //console.log(data.length / rawData.length);
        let response: SaveResponseDTO = await componentRef.invokeMethodAsync("Save", data);
        //let response: SaveResponseDTO = await componentRef.invokeMethodAsync("Save", writepad.LastModified, newDrawings, validDeletedDrawings);
        switch (response.statusCode) {
            case StatusCode.SuccessOK:
                deletedDrawings.length = 0;
                recoveredDrawings.length = 0;
                writepad.LastModified = response.lastModified;
                if (response.lastSavedDrawingNumber != 0) {
                    writepad.LastSavedDrawingNumber = response.lastSavedDrawingNumber;
                }
                break;
            default:
                if (response.throwError) {
                    throw new Error("Couldn't Save!");
                }
                break;
        }
        //console.log("End!" + new Date());
    }
    catch {
        succeed = false;
    }
    finally {
        isSaving = false;
        await componentRef.invokeMethodAsync("ReleaseSaveToken");
    }

    return succeed;
}

async function updateDotNetUndoRedo() {
    let undo = writepad.Points.length != 0;
    let redo = undoStack.length != 0;

    await componentRef.invokeMethodAsync("UndoRedoUpdator", undo, redo);
}

export function undo() {
    if (isMiddleOfDrawing || writepad.Points.length == 0) {
        return;
    }
    let lastStartingIndex = _.findLastIndex(writepad.Points, (p: Point) => p.Type == PointType.Starting);
    let deletedPoints = writepad.Points.splice(lastStartingIndex);
    undoStack.push(deletedPoints);
    let drawingRange = {
        StartingNumber: deletedPoints[0].Number,
        EndingNumber: _.last(deletedPoints).Number
    };
    deletedDrawings.push(drawingRange);
    recoveredDrawings = recoveredDrawings.filter(d => d.StartingNumber != drawingRange.StartingNumber);
    redraw();

    updateDotNetUndoRedo();
}

export function redo() {
    if (isMiddleOfDrawing || undoStack.length == 0) {
        return;
    }
    let deletedPoints = undoStack.pop();
    deletedPoints.forEach((point) => writepad.Points.push(point));
    recoveredDrawings.push({
        StartingNumber: deletedPoints[0].Number,
        EndingNumber: _.last(deletedPoints).Number
    });
    deletedDrawings.pop();
    redraw();

    updateDotNetUndoRedo();
}

function toFixedNumber(number: number, digits: number, base = 10): number {
    let power = Math.pow(base, digits);
    return Math.round(number * power) / power;
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
    let lastX: number;
    let lastY: number;
    let moved: boolean;

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
                if (_.findIndex(selectedDrawings, (d: DrawingRange) => d.StartingNumber == p.Number) == -1) {
                    context.strokeStyle = "black";
                }
                else {
                    context.strokeStyle = "red";
                }

                context.beginPath();
                context.moveTo(screenX, screenY);
                moved = false;
                break;
            case PointType.Middle:
                if (lastX != screenX || lastY != screenY) {
                    context.lineTo(screenX, screenY);
                    moved = true;
                }
                break;
            case PointType.Ending:
                if (isInside) {
                    if (lastX != screenX || lastY != screenY) {
                        context.lineTo(screenX, screenY);
                        moved = true;
                    }
                    if (moved) {
                        context.stroke();
                    }
                    else {
                        context.fillRect(screenX-1, screenY-1, 2, 2);
                    }
                    isInside = false;
                }
                break;
        }

        lastX = screenX;
        lastY = screenY;
    }

    if (selectionBox) {
        context.setLineDash([1, 1]);
        context.lineWidth = 1;
        let boxMinX = toScreenX(selectionBox.MinX);
        let boxMaxX = toScreenX(selectionBox.MaxX);
        let boxMinY = toScreenY(selectionBox.MinY);
        let boxMaxY = toScreenY(selectionBox.MaxY);
        context.beginPath();
        context.moveTo(boxMinX, boxMinY);
        context.lineTo(boxMinX, boxMaxY);
        context.lineTo(boxMaxX, boxMaxY);
        context.lineTo(boxMaxX, boxMinY);
        context.lineTo(boxMinX, boxMinY);
        context.stroke();
    }

    initContext();
}

function toRealX(screenX: number): number {
    return screenX - offsetX;
}

function toRealY(screenY: number): number {
    return screenY - offsetY;
}

async function onKeyUp(event: KeyboardEvent) {
    if (event.ctrlKey && !event.shiftKey && event.keyCode == 90) { // Ctrl + Z
        undo();
    }
    else if (event.ctrlKey && ((event.shiftKey && event.keyCode == 90) || event.keyCode == 89)) { // Ctrl + Shift + Z || Ctrl + Y
        redo();
    }
    else if (event.keyCode == 77) { // M
        let newMode = Mode.Move;
        changeDefaultMode(newMode)
        componentRef.invokeMethodAsync("DefaultModeUpdator", newMode);
    }
    else if (event.keyCode == 68) { // D
        let newMode = Mode.Non;
        changeDefaultMode(newMode)
        componentRef.invokeMethodAsync("DefaultModeUpdator", newMode);
    }
    else if (event.keyCode == 83) { // S
        let newMode = Mode.Select;
        changeDefaultMode(newMode)
        componentRef.invokeMethodAsync("DefaultModeUpdator", newMode);
    }
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

function addToDrawings(): void {
    let shouldUpdateUndoRedo = writepad.Points.length == 0 || undoStack.length != 0;

    Array.prototype.push.apply(writepad.Points, tempPoints);

    if (shouldUpdateUndoRedo) {
        undoStack.length = 0;
        updateDotNetUndoRedo();
    }

    if (selectedDrawings.length != 0) {
        selectedDrawings.length = 0;
    }

    num = num + count;
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

    if (event.button == 0 && event.buttons == 1 //Left Mouse, Touch Contact, Pen Contact
        && detectPointerType(event.pointerType) == writepad.PointerType) {
        if (event.isPrimary) {
            return Mode.Draw;
        }
        else {
            return Mode.Move;
        }
    }
    else if (mode == Mode.Non) {
        return Mode.Move;
    }

    return mode;
}

async function draw(startX, startY, endX, endY, drawDot: boolean = false) {
    if (startX != endX || startY != endY) {
        context.beginPath();
        context.moveTo(startX, startY);
        context.lineTo(endX, endY);
        context.stroke();
    }
    else if (drawDot) {
        context.fillRect(endX-1, endY-1, 2, 2);
    }
}

function onPointerDown(event: PointerEvent) {
    event.preventDefault();

    if (firstPointerTime && event.timeStamp - firstPointerTime > 250) {
        return
    }

    mode = detectMode(event);

    if (isMiddleOfDrawing) {
        return;
    }
    isMiddleOfDrawing = true;

    pointerId = event.pointerId;
    firstPointerTime = event.timeStamp;

    pointerX = prevPointerX = event.clientX - horizontalOffset;
    pointerY = prevPointerY = event.clientY - verticalOffset;
    const realX = toRealX(pointerX);
    const realY = toRealY(pointerY);

    switch (mode) {
        case Mode.Draw:
            count++;
            lastPoint = createPoint(event, PointType.Starting, realX, realY, num + count);
            tempPoints.push(lastPoint);
            break;
        case Mode.Move:
            lastMoveTime = event.timeStamp;
            break;
        case Mode.Select:
            selectionBox = {
                StartingX: realX,
                StartingY: realY
            };
            lastMoveTime = event.timeStamp;
            break;
    }
}

function onPointerMove(event: PointerEvent) {
    event.preventDefault();

    if (pointerId != event.pointerId)
        return;

    pointerX = event.clientX - horizontalOffset;
    pointerY = event.clientY - verticalOffset;
    const realX = toRealX(pointerX);
    const realY = toRealY(pointerY);

    switch (mode) {
        case Mode.Draw:
            count++;
            draw(prevPointerX, prevPointerY, pointerX, pointerY);
            lastPoint = createPoint(event, PointType.Middle, realX, realY, num + count);
            tempPoints.push(lastPoint);
            break;
        case Mode.Move:
            offsetX += pointerX - prevPointerX;
            offsetY += pointerY - prevPointerY;
            if (!lastMoveTime || event.timeStamp - lastMoveTime > 200) {
                lastMoveTime = event.timeStamp;
                redraw();
            }
            break;
        case Mode.Select:
            if (!lastMoveTime || event.timeStamp - lastMoveTime > 200) {
                updateSelectionBox(realX, realY);
                lastMoveTime = event.timeStamp;
                select();
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

    pointerX = event.clientX - horizontalOffset;
    pointerY = event.clientY - verticalOffset;
    const realX = toRealX(pointerX);
    const realY = toRealY(pointerY);

    switch (mode) {
        case Mode.Draw:
            count++;
            draw(prevPointerX, prevPointerY, pointerX, pointerY, true);
            lastPoint = createPoint(event, PointType.Ending, realX, realY, num + count);
            tempPoints.push(lastPoint);

            addToDrawings();
            break;
        case Mode.Move:
            redraw();
            lastMoveTime = undefined;
            break;
        case Mode.Select:
            updateSelectionBox(realX, realY);
            select();
            selectionBox = undefined;
            redraw();
            lastMoveTime = undefined;
            break;
    }

    mode = defaultMode;
    firstPointerTime = undefined;
    pointerId = undefined;
    count = 0;
    tempPoints.length = 0;
    isMiddleOfDrawing = false;

    if (saveInQueue) {
        saveInQueue = false;
        save().catch(error => {
            console.error(error);
            throw error;
        });
    }
}

function updateSelectionBox(x: number, y: number): void {
    if (x > selectionBox.StartingX) {
        selectionBox.MinX = selectionBox.StartingX;
        selectionBox.MaxX = x;
    }
    else {
        selectionBox.MinX = x;
        selectionBox.MaxX = selectionBox.StartingX;
    }
    if (y > selectionBox.MinY) {
        selectionBox.MinY = selectionBox.StartingY;
        selectionBox.MaxY = y;
    }
    else {
        selectionBox.MinY = y;
        selectionBox.MaxY = selectionBox.StartingY;
    }
}

function select(): void {
    selectedDrawings.length = 0;

    let startingNumber: number;
    let isInside: boolean = false;
    for (let p of writepad.Points) {
        let insideCheck =
            p.X >= selectionBox.MinX && p.X <= selectionBox.MaxX &&
            p.Y >= selectionBox.MinY && p.Y <= selectionBox.MaxY;
        if (!isInside && insideCheck) {
            isInside = true;
        }

        switch (p.Type) {
            case PointType.Starting:
                startingNumber = p.Number;
                if (isInside && !insideCheck) {
                    isInside = false;
                }
                break;
            case PointType.Ending:
                if (isInside) {
                    selectedDrawings.push({
                        StartingNumber: startingNumber,
                        EndingNumber: p.Number
                    });
                    isInside = false;
                }
                break;
        }
    }
}

interface SelectionBox {
    StartingX: number;
    StartingY: number;
    MinX?: number,
    MinY?: number,
    MaxX?: number,
    MaxY?: number
}

interface SavePointsDTO {
    LastModified: Date,
    NewPoints: Point[],
    DeletedDrawings: DrawingRange[],
    RecoveredDrawings: DrawingRange[]
}

interface SaveResponseDTO {
    statusCode: StatusCode,
    throwError: boolean,
    lastModified: Date,
    lastSavedDrawingNumber: number
}

interface Writepad {
    LastModified: Date;
    LastSavedDrawingNumber: number;
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

interface DrawingRange {
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
    Move,
    Select
}
