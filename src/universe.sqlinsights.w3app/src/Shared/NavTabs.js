import React, {useRef} from 'react';
import PropTypes from 'prop-types';
import { makeStyles } from '@material-ui/core/styles';
import AppBar from '@material-ui/core/AppBar';
import Tabs from '@material-ui/core/Tabs';
import Tab from '@material-ui/core/Tab';
import Typography from '@material-ui/core/Typography';
import Box from '@material-ui/core/Box';

import ActionsTab from "../Actions/ActionsTab";
import * as DocumentVisibilityStore from "../Shared/DocumentVisibilityStore"
import AboutPanel from "../About/AboutPanel";

function TabPanel(props) {
    const { children, value, index, ...other } = props;

    return (
        <div
            role="tabpanel"
            hidden={value !== index}
            id={`simple-tabpanel-${index}`}
            aria-labelledby={`simple-tab-${index}`}
            {...other}
        >
            {value === index && (
                <Box p={3}>
                    <Typography>{children}</Typography>
                </Box>
            )}
        </div>
    );
}

TabPanel.propTypes = {
    children: PropTypes.node,
    index: PropTypes.any.isRequired,
    value: PropTypes.any.isRequired,
};

function a11yProps(index) {
    return {
        id: `simple-tab-${index}`,
        'aria-controls': `simple-tabpanel-${index}`,
    };
}

const useStyles = makeStyles((theme) => ({
    root: {
        flexGrow: 1,
        backgroundColor: theme.palette.background.paper,
        padding: 0,
    },
}));

const tabPanelStyles = makeStyles((theme) => ({
    root: {
        padding: 4,
    },
}));

let refAll;

DocumentVisibilityStore.on(isVisible => {
    if (refAll && refAll.current) {
        // TODO: blur on lost visibility
        // console.log("refAll", refAll);
        // refAll.current.style.opacity = isVisible ? 1 : 0.5;
    }
});

export default function SimpleTabs() {
    const classes = useStyles();
    const [value, setValue] = React.useState(0);

    const handleChange = (event, newValue) => {
        setValue(newValue);
    };
    
    refAll = useRef();

    return (
        
        <div className={classes.root} ref={refAll}>
            <AppBar position="static" className="AppBar">
                <Tabs value={value} onChange={handleChange} aria-label="simple tabs example">
                    <Tab label="Endpoints & Tasks" {...a11yProps(0)} />
                    <Tab label="Errors" {...a11yProps(1)} className="hidden" />
                    <Tab label="About" {...a11yProps(2)}  />
                </Tabs>
            </AppBar>
            <TabPanel value={value} index={0} className={tabPanelStyles.root}>
                <ActionsTab />
            </TabPanel>
            <TabPanel value={value} index={1} className={"hidden"}>
                Item Two
            </TabPanel>
            <TabPanel value={value} index={2}>
                <AboutPanel />
            </TabPanel>
        </div>
    );
}
