import { ReactComponent as SvgIconRename } from './Rename.svg';
import { ReactComponent as SvgIconStop } from './Stop.svg';
import { ReactComponent as SvgIconDelete } from './Delete.svg';
import { ReactComponent as SvgIconResume } from './Resume.svg';
import React from "react";

const defaultSize = 15, defaultColor='#333';

function CreateIcon(size = defaultSize, color = "#444", Svg) {
    return <Svg style={{width: size,height:size,fill:color,strokeWidth:'1px',stroke:color }} />;
}

export const IconRename = (size=defaultSize,color='#333') => (<SvgIconRename style={{width: size,height:size,fill:color,strokeWidth:'1px',stroke:color }} />);
export const IconStop = (size=defaultSize,color='#333') => (<SvgIconStop style={{width: size,height:size,fill:color,strokeWidth:'1px',stroke:color }} />);
export const IconDelete = (size=defaultSize,color='#333') => (<SvgIconDelete style={{width: size,height:size,fill:color,strokeWidth:'1px',stroke:color }} />);
export const IconResume = (size=defaultSize,color='#333') => (<SvgIconResume style={{width: size,height:size,fill:color,strokeWidth:'1px',stroke:color }} />);

