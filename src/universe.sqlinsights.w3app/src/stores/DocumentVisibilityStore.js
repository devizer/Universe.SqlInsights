import * as Helper from "../Helper"

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
    console.log("SUBSRIBING to [visibilitychange]");
    document.addEventListener("visibilitychange", () => {
        const isHidden = Helper.isDocumentHidden();
        console.log(`DOCUMENT VISIBLILITY TRIGGERED: visible=${!isHidden}, visibility=${document.visibilityState}`);
        notifyAll(!isHidden);
    }, false);
}
else {
    console.warn("Unable to SUBSCRIBE to document[visibilitychange]");
}
