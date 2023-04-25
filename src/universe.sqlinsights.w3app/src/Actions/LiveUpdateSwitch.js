import Switch from "@material-ui/core/Switch";
import React from "react";
import { ReactComponent as PlayIconSvg } from './Play.svg';
import { ReactComponent as PauseIconSvg } from './Pause.svg';
import PropTypes from "prop-types";

const PlayIcon = (size=20,color='#333') => (<PlayIconSvg style={{width: size,height:size,fill:color,strokeWidth:'1px',stroke:color }} />);
const PauseIcon = (size=20,color='#333') => (<PauseIconSvg style={{width: size,height:size,fill:color,strokeWidth:'1px',stroke:color }} />);

export function LiveUpdateSwitch({autoUpdateSummary,handleAutoUpdateSummary}) {
    return ( 
    <Switch
        checked={autoUpdateSummary}
        onChange={handleAutoUpdateSummary}
        color="default"
        name="autoUpdateSummary"
        inputProps={{'aria-label': 'auto update summary', title: "Auto Update Summary"}}
        checkedIcon={PlayIcon()}
        icon={PauseIcon()}
    />
    );
}

LiveUpdateSwitch.propTypes = {
    autoUpdateSummary: PropTypes.bool,
    handleAutoUpdateSummary: PropTypes.func
};

