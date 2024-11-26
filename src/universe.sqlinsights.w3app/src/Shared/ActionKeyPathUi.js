
export function ActionKeyPathUi({path}) {
    // return JoinString(" \u2192 ", path);
    // console.log('ARG PATH: ', path);
    return path ? path.join(" \u2192 ") : "";
}

