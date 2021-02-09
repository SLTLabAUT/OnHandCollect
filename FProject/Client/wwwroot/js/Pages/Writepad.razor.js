var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
import * as _ from "/lib/lodash-es/lodash.js";
const canvas = document.getElementById("writepad");
const context = canvas.getContext("2d");
let componentRef;
const drawings = [];
let isSaving = false;
let lastSavedDrawing = -1;
const undoStack = [];
let num = 0;
let pointerX;
let pointerY;
let prevPointerX;
let prevPointerY;
let offsetX = 0;
let offsetY = 0;
let lastMoveTime = undefined;
let mode = 0 /* Non */;
export function init(compRef) {
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
export function save() {
    return __awaiter(this, void 0, void 0, function* () {
        if (isSaving) {
            return;
        }
        try {
            isSaving = true;
            let startingIndex = _.findLastIndex(drawings, (p) => p.Number <= lastSavedDrawing) + 1;
            let endingIndex = _.findLastIndex(drawings, (p) => p.Type == 2 /* Ending */ && p.Number > lastSavedDrawing);
            if (endingIndex == -1) {
                return;
            }
            let newDrawings = drawings.slice(startingIndex, endingIndex + 1);
            yield componentRef.invokeMethodAsync("Save", newDrawings);
            lastSavedDrawing = drawings[endingIndex].Number;
        }
        finally {
            isSaving = false;
        }
    });
}
export function undo() {
    if (_.last(drawings).Type != 2 /* Ending */) {
        throw new Error("Drawing is in progress!");
    }
    let lastStarting = _.findLastIndex(drawings, (p) => p.Type == 1 /* Starting */);
    let deletedDrawings = drawings.splice(lastStarting);
    undoStack.push(deletedDrawings);
    redraw();
}
export function redo() {
    if (_.last(drawings).Type != 2 /* Ending */) {
        throw new Error("Drawing is in progress!");
    }
    let deletedDrawings = undoStack.pop();
    deletedDrawings.forEach((point) => drawings.push(point));
    redraw();
}
function toFixedNumber(number, digits, base = 10) {
    let power = Math.pow(base, digits);
    return Math.round(number * power) / power;
}
function toScreenX(realX) {
    return realX + offsetX;
}
function toScreenY(realY) {
    return realY + offsetY;
}
function redraw() {
    canvas.width = window.innerWidth;
    canvas.height = window.innerHeight;
    context.clearRect(0, 0, canvas.width, canvas.height);
    for (let d of drawings) {
        switch (d.Type) {
            case 1 /* Starting */:
                context.beginPath();
                context.moveTo(toScreenX(d.X), toScreenY(d.Y));
                break;
            case 0 /* Middle */:
            case 2 /* Ending */:
                context.lineTo(toScreenX(d.X), toScreenY(d.Y));
                break;
        }
        context.stroke();
    }
}
function toRealX(screenX) {
    return screenX - offsetX;
}
function toRealY(screenY) {
    return screenY - offsetY;
}
function addToDrawings(event, type, x, y) {
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
function onPointerDown(event) {
    if (event.button == 0 && event.buttons == 1) { //Left Mouse, Touch Contact, Pen contact
        mode = 1 /* Draw */;
    }
    else if (event.button == 2 && event.buttons == 2) { //Right Mouse, Pen barrel button
        mode = 3 /* Move */;
    }
    pointerX = prevPointerX = event.clientX;
    pointerY = prevPointerY = event.clientY;
    switch (mode) {
        case 1 /* Draw */:
            context.beginPath();
            context.moveTo(pointerX, pointerY);
            addToDrawings(event, 1 /* Starting */, toRealX(pointerX), toRealY(pointerY));
            break;
        case 3 /* Move */:
            lastMoveTime = event.timeStamp;
            break;
    }
}
function onPointerMove(event) {
    pointerX = event.clientX;
    pointerY = event.clientY;
    switch (mode) {
        case 1 /* Draw */:
            context.lineTo(pointerX, pointerY);
            context.stroke();
            const last = _.last(drawings);
            const realX = toRealX(pointerX);
            const realY = toRealY(pointerY);
            if (last.Type != 1 /* Starting */
                || last.Time != event.timeStamp
                || last.X != realX || last.Y != realY) {
                addToDrawings(event, 0 /* Middle */, realX, realY);
            }
            break;
        case 3 /* Move */:
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
function onPointerUp(event) {
    let lastMode = mode;
    mode = 0 /* Non */;
    pointerX = event.clientX;
    pointerY = event.clientY;
    switch (lastMode) {
        case 1 /* Draw */:
            let last = _.last(drawings);
            const realX = toRealX(pointerX);
            const realY = toRealY(pointerY);
            if (last.Time != event.timeStamp
                || last.X != realX || last.Y != realY) {
                context.lineTo(pointerX, pointerY);
                context.stroke();
                addToDrawings(event, 2 /* Ending */, realX, realY);
            }
            else {
                last.Type = 2 /* Ending */;
            }
            break;
        case 3 /* Move */:
            redraw();
            lastMoveTime = undefined;
            break;
    }
}
//# sourceMappingURL=Writepad.razor.js.map