
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

