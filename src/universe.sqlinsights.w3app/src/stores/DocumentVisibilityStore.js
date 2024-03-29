﻿import * as Helper from "../Helper"

const listeners = [];

export const on = listener => {
    listeners.push(listener);
}

export const off = listener => {
    var index = listeners.indexOf(listener);
    if (index !== -1) {
        listeners.splice(index, 1);
    }    
}

const notifyAll = isVisible => {
    listeners.forEach(listener => {
        if (listener) listener(isVisible);
    });
}

if (document && document.visibilityState && typeof document.onvisibilitychange !== undefined) {
    console.log("SUBRIBING to [visibilitychange]");
    document.addEventListener("visibilitychange", () => {
        const isHidden = Helper.isDocumentHidden();
        console.log(`VISIBLILITY: ${!isHidden}, ${document.visibilityState}`);
        notifyAll(!isHidden);
    }, false);
}
else {
    console.warn("Unable to SUBSRIBE to document[visibilitychange]");
}
