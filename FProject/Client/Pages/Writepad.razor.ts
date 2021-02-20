import * as _ from "/lib/lodash-es/lodash.js";

const canvas = <HTMLCanvasElement>document.getElementById("writepad");
const context = canvas.getContext("2d");

let componentRef;
let writepad: Writepad;
let isSaving = false;
let lastSavedDrawingNumber: number = -1;
const undoStack: Point[][] = [];
const deletedDrawings: DeletedDrawing[] = [];
let num = 0;
let pointerX;
let pointerY;
let prevPointerX;
let prevPointerY;
let offsetX = 0;
let offsetY = 0;
let lastMoveTime: number = undefined;
let mode: Mode = Mode.Non;
let pointerId: number;

export function init(compRef, writepadCompressedJson: string): void {
    componentRef = compRef;
    let writepadReceived = JSON.parse(Decompress(writepadCompressedJson));
    writepad = {
        LastModified: writepadReceived.LastModified,
        PointerType: writepadReceived.PointerType,
        Text: {
            Type: writepadReceived.Text.Type,
            Content: writepadReceived.Text.Content
        },
        Points: writepadReceived.Points ?? []
    };
    if (writepad.Points.length != 0) {
        lastSavedDrawingNumber = _.last(writepad.Points).Number;
        num = lastSavedDrawingNumber + 1;
    }

    redraw();

    window.addEventListener("resize", redraw);

    canvas.addEventListener("contextmenu", e => e.preventDefault());
    canvas.addEventListener("pointerdown", onPointerDown);
    canvas.addEventListener("pointermove", onPointerMove);
    canvas.addEventListener("pointerup", onPointerUp);
    canvas.addEventListener("pointercancel", onPointerUp);
    canvas.addEventListener("pointerleave", onPointerUp);
}

export async function save(): Promise<void> {
    if (isSaving) {
        return;
    }

    try {
        isSaving = true;

        let startingIndex = _.findLastIndex(writepad.Points, (p: Point) => p.Number <= lastSavedDrawingNumber) + 1;
        let endingIndex = _.findLastIndex(writepad.Points, (p: Point) => p.Type == PointType.Ending && p.Number > lastSavedDrawingNumber);
        if (endingIndex == -1) {
            return;
        }

        console.log("Start!" + new Date());
        let validDeletedDrawings = deletedDrawings.filter(d => d.StartingNumber <= lastSavedDrawingNumber);

        let newDrawings = writepad.Points.slice(startingIndex, endingIndex + 1);

        console.log("Middle1!" + new Date());
        let savePointsDTO: SavePointsDTO = {
            LastModified: writepad.LastModified,
            NewPoints: newDrawings,
            DeletedDrawings: validDeletedDrawings
        };
        let rawData = JSON.stringify(savePointsDTO);
        let data = Compress(rawData);
        console.log(data.length / rawData.length);
        let response: SaveResponseDTO = await componentRef.invokeMethodAsync("Save", data);
        //let response: SaveResponseDTO = await componentRef.invokeMethodAsync("Save", writepad.LastModified, newDrawings, validDeletedDrawings);
        switch (response.statusCode) {
            case StatusCode.ClientErrorNotFound:
            case StatusCode.ClientErrorBadRequest:
                break;
            case StatusCode.SuccessOK:
                writepad.LastModified = JSON.parse(response.jsonContent).LastModified;
                lastSavedDrawingNumber = _.last(newDrawings).Number;
                break;
            default:
                break;
        }
        console.log("End!" + new Date());
    } finally {
        isSaving = false;
    }
}

interface SavePointsDTO {
    LastModified: Date,
    NewPoints: Point[],
    DeletedDrawings: DeletedDrawing[]
}

interface SaveResponseDTO {
    statusCode: StatusCode,
    jsonContent: string
}

export function undo() {
    if (_.last(writepad.Points).Type != PointType.Ending) {
        throw new Error("Drawing is in progress!")
    }
    let lastStartingIndex = _.findLastIndex(writepad.Points, (p: Point) => p.Type == PointType.Starting);
    let deletedPoints = writepad.Points.splice(lastStartingIndex);
    undoStack.push(deletedPoints);
    deletedDrawings.push({
        StartingNumber: lastStartingIndex,
        EndingNumber: _.last(deletedPoints).Number
    });
    redraw();
}

export function redo() {
    if (_.last(writepad.Points).Type != PointType.Ending) {
        throw new Error("Drawing is in progress!")
    }
    if (undoStack.length == 0) {
        return;
    }
    let deletedPoints = undoStack.pop();
    deletedPoints.forEach((point) => writepad.Points.push(point));
    deletedDrawings.pop();
    redraw();
}

function toFixedNumber(number: number, digits: number, base = 10): number {
    let power = Math.pow(base, digits);
    return Math.round(number * power) / power;
}

interface Writepad {
    LastModified: Date;
    readonly PointerType: PointerType;
    readonly Text: WritepadText;
    readonly Points: Point[];
}

interface WritepadText {
    readonly Type: TextType;
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

const enum TextType {
    Text,
    WordGroups
}

const enum PointerType {
    Mouse,
    Touchpad,
    Pen,
    Touch
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

function toScreenX(realX: number): number {
    return realX + offsetX;
}

function toScreenY(realY: number): number {
    return realY + offsetY;
}

function redraw(): void {
    canvas.width = window.innerWidth;
    canvas.height = window.innerHeight;
    let minX = - canvas.width;
    let maxX = 2 * canvas.width;
    let minY = - canvas.height;
    let maxY = 2 * canvas.height;
    let isInside: boolean;

    context.clearRect(0, 0, canvas.width, canvas.height);
    for (let d of writepad.Points) {
        let screenX = toScreenX(d.X);
        let screenY = toScreenY(d.Y);

        if (!isInside &&
            screenX > minX && screenX < maxX &&
            screenY > minY && screenY < maxY) {
            isInside = true;
        }

        switch (d.Type) {
            case PointType.Starting:
                isInside = false;
                context.beginPath();
                context.moveTo(screenX, screenY);
                break;
            case PointType.Middle:
                context.lineTo(screenX, screenY);
                break;
            case PointType.Ending:
                context.lineTo(screenX, screenY);
                if (isInside)
                    context.stroke();
                break;
        }
    }
}

function toRealX(screenX: number): number {
    return screenX - offsetX;
}

function toRealY(screenY: number): number {
    return screenY - offsetY;
}

function addToDrawings(event: PointerEvent, type: PointType, x: number, y: number): void {
    writepad.Points.push({
        Number: num,
        Type: type,
        TimeStamp: toFixedNumber(event.timeStamp, 3),
        X: toFixedNumber(x, 5),
        Y: toFixedNumber(y, 5),
        Width: toFixedNumber(event.width, 3),
        Height: toFixedNumber(event.height, 3),
        Pressure: toFixedNumber(event.pressure, 5),
        TangentialPressure: toFixedNumber(event.tangentialPressure, 5),
        TiltX: toFixedNumber(event.tiltX, 3),
        TiltY: toFixedNumber(event.tiltY, 3),
        Twist: Math.round(event.twist)
    });
    num++;
}

function detectPointerType(type: string): PointerType {
    switch (type) {
        case "mouse":
            if (writepad.PointerType == PointerType.Mouse || writepad.PointerType == PointerType.Touchpad)
                return writepad.PointerType;
            else return PointerType.Mouse;
        case "touch":
            return PointerType.Touch;
        case "pen":
            return PointerType.Pen;
        default:
            return PointerType.Mouse;
    }
}
// TODO: Check touchpad and mouse at the beginning https://stackoverflow.com/questions/10744645/detect-touchpad-vs-mouse-in-javascript/62415754#62415754

function onPointerDown(event: PointerEvent) {
    if (mode != Mode.Non) {
        return;
    }

    if (event.button == 0 && event.buttons == 1 && //Left Mouse, Touch Contact, Pen contact
        detectPointerType(event.pointerType) == writepad.PointerType &&
        event.isPrimary) {
        mode = Mode.Draw;
    } else {
        mode = Mode.Move;
    }

    pointerId = event.pointerId;

    pointerX = prevPointerX = event.clientX;
    pointerY = prevPointerY = event.clientY;

    switch (mode) {
        case Mode.Draw:
            undoStack.length = 0;
            addToDrawings(event, PointType.Starting, toRealX(pointerX), toRealY(pointerY));
            break;
        case Mode.Move:
            lastMoveTime = event.timeStamp;
            break;
    }
}

function onPointerMove(event: PointerEvent) {
    if (pointerId != event.pointerId)
        return;

    pointerX = event.clientX;
    pointerY = event.clientY;

    switch (mode) {
        case Mode.Draw:
            const last = _.last(writepad.Points);
            const realX = toRealX(pointerX);
            const realY = toRealY(pointerY);
            if (last.Time != event.timeStamp
                || last.X != realX || last.Y != realY) {
                context.beginPath();
                context.moveTo(prevPointerX, prevPointerY);
                context.lineTo(pointerX, pointerY);
                context.stroke();
                addToDrawings(event, PointType.Middle, realX, realY);
            }
            break;
        case Mode.Move:
            offsetX += pointerX - prevPointerX;
            offsetY += pointerY - prevPointerY;
            if (event.timeStamp - lastMoveTime > 250) {
                lastMoveTime = event.timeStamp;
                redraw();
            }
            break;
    }

    prevPointerX = pointerX;
    prevPointerY = pointerY;
}

function onPointerUp(event: PointerEvent) {
    if (pointerId != event.pointerId)
        return;

    pointerX = event.clientX;
    pointerY = event.clientY;

    switch (mode) {
        case Mode.Draw:
            let last = _.last(writepad.Points);
            const realX = toRealX(pointerX);
            const realY = toRealY(pointerY);
            if (last.Type == PointType.Starting
                || last.Time != event.timeStamp
                || last.X != realX || last.Y != realY) {
                context.beginPath();
                context.moveTo(prevPointerX, prevPointerY);
                context.lineTo(pointerX, pointerY);
                context.stroke();
                addToDrawings(event, PointType.Ending, realX, realY);
            } else {
                last.Type = PointType.Ending;
            }
            break;
        case Mode.Move:
            redraw();
            lastMoveTime = undefined;
            break;
    }

    mode = Mode.Non;
}
