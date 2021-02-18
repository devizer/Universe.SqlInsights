import {API_URL} from "./stores/DataSourceListener";

export const toConsole = function(caption, obj) {
    if (process.env.NODE_ENV !== 'production') {
        console.log(`%c--===**** ${caption} ****===--`, "color: #37603E", obj);
    }
}

export const isDocumentHidden = () => {
    // https://www.w3.org/TR/page-visibility-2/#idl-def-document-visibilitystate
    let isHidden = false;
    if (document && document.visibilityState && document.visibilityState !== 'visible') { isHidden = true; }
    return isHidden;
}


export const keyPathSeparator = {
    char: '→',
    code: 0x2192
};

export function createRequest(action, body) {
    return new Request(`${API_URL}/${action}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'Accept': 'application/json' },
        /*
                    mode: 'cors',
        headers: { 'Content-Type': 'application/json', 'Accept': 'application/json' }, 
                    redirect: 'follow', 
        */
        body: JSON.stringify(body)
    });
}
