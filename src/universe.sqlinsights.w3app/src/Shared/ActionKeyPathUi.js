
export function ActionKeyPathUi({path}) {
    // return JoinString(" \u2192 ", path);
    // console.log('ARG PATH: ', path);
    return path ? path.join(" \u2192 ") : "";
}

export function ConvertToSafeFileName(keyPath) {
    const ret = [];
    const len = keyPath.length;
    let isOpen = true;
    for (let i = 0; i < keyPath.length; i++) {
        const c = keyPath.charAt(i);
        const cNext = i + 1 >= len ? null : keyPath.charAt(i+1);  
        if (c === '\\') ret.push("\u29F5");
        else if (c === '/') ret.push("\u2215");
        else if (c === ':') ret.push("\uA789");
        else if (c === '"') {
            if (isOpen) ret.push("\u201C"); else ret.push("\u201D");
            isOpen = !isOpen;
        }
        else 
            ret.push(c);
    }
    
    return ret.join('')
}

/*
let demo1 = ["Hi", "Hi:AAA\\BB\\C(\"pie\")"].map(x => `${x} ==> ${ConvertToSafeFileName(x)}`);
console.log(demo1.join("\r\n"));
*/