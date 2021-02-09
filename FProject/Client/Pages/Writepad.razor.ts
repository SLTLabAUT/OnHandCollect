import * as _ from "/lib/lodash-es/lodash.js";

const canvas = <HTMLCanvasElement>document.getElementById("writepad");
const context = canvas.getContext("2d");

let componentRef;
const drawings: Point[] = [];
let isSaving = false;
let lastSavedDrawing: number = -1;
const undoStack: Point[][] = [];
let num = 0;
let pointerX;
let pointerY;
let prevPointerX;
let prevPointerY;
let offsetX = 0;
let offsetY = 0;
let lastMoveTime: number = undefined;
let mode: Mode = Mode.Non;

export function init(compRef): void {
    componentRef = compRef;

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

        let startingIndex = _.findLastIndex(drawings, (p: Point) => p.Number <= lastSavedDrawing) + 1;
        let endingIndex = _.findLastIndex(drawings, (p: Point) => p.Type == PointType.Ending && p.Number > lastSavedDrawing);
        if (endingIndex == -1) {
            return;
        }

        let newDrawings = drawings.slice(startingIndex, endingIndex + 1);
        await componentRef.invokeMethodAsync("Save", newDrawings);
        lastSavedDrawing = drawings[endingIndex].Number;
    } finally {
        isSaving = false;
    }
}

export function undo() {
    if (_.last(drawings).Type != PointType.Ending) {
        throw new Error("Drawing is in progress!")
    }
    let lastStarting = _.findLastIndex(drawings, (p: Point) => p.Type == PointType.Starting);
    let deletedDrawings = drawings.splice(lastStarting);
    undoStack.push(deletedDrawings);
    redraw();
}

export function redo() {
    if (_.last(drawings).Type != PointType.Ending) {
        throw new Error("Drawing is in progress!")
    }
    let deletedDrawings = undoStack.pop();
    deletedDrawings.forEach((point) => drawings.push(point));
    redraw();
}

function toFixedNumber(number: number, digits: number, base = 10): number {
    let power = Math.pow(base, digits);
    return Math.round(number * power) / power;
}

interface Point {
    readonly Number: number;
    Type: PointType;
    readonly Time: number;
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

    context.clearRect(0, 0, canvas.width, canvas.height);
    for (let d of drawings) {
        switch (d.Type) {
            case PointType.Starting:
                context.beginPath();
                context.moveTo(toScreenX(d.X), toScreenY(d.Y));
                break;
            case PointType.Middle:
            case PointType.Ending:
                context.lineTo(toScreenX(d.X), toScreenY(d.Y));
                break;
        }
        context.stroke();
    }
}

function toRealX(screenX: number): number {
    return screenX - offsetX;
}

function toRealY(screenY: number): number {
    return screenY - offsetY;
}

function addToDrawings(event: PointerEvent, type: PointType, x: number, y: number): void {
    drawings.push({
        Number: num,
        Type: type,
        Time: toFixedNumber(event.timeStamp, 3),
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

function onPointerDown(event: PointerEvent) {
    if (event.button == 0 && event.buttons == 1) { //Left Mouse, Touch Contact, Pen contact
        mode = Mode.Draw;
    } else if (event.button == 2 && event.buttons == 2) { //Right Mouse, Pen barrel button
        mode = Mode.Move;
    }

    pointerX = prevPointerX = event.clientX;
    pointerY = prevPointerY = event.clientY;

    switch (mode) {
        case Mode.Draw:
            context.beginPath();
            context.moveTo(pointerX, pointerY);
            addToDrawings(event, PointType.Starting, toRealX(pointerX), toRealY(pointerY));
            break;
        case Mode.Move:
            lastMoveTime = event.timeStamp;
            break;
    }
}

function onPointerMove(event: PointerEvent) {
    pointerX = event.clientX;
    pointerY = event.clientY;

    switch (mode) {
        case Mode.Draw:
            context.lineTo(pointerX, pointerY);
            context.stroke();
            const last = _.last(drawings);
            const realX = toRealX(pointerX);
            const realY = toRealY(pointerY);
            if (last.Type != PointType.Starting
                || last.Time != event.timeStamp
                || last.X != realX || last.Y != realY) {
                addToDrawings(event, PointType.Middle, realX, realY);
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
    let lastMode = mode;
    mode = Mode.Non;

    pointerX = event.clientX;
    pointerY = event.clientY;

    switch (lastMode) {
        case Mode.Draw:
            let last = _.last(drawings);
            const realX = toRealX(pointerX);
            const realY = toRealY(pointerY);
            if (last.Time != event.timeStamp
                || last.X != realX || last.Y != realY) {
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
}
