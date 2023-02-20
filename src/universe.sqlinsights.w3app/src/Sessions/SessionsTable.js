import * as Helper from "../Helper"
import React, { Component } from 'react';
import sessionsStore from "../stores/SessionsStore";
import {ActionKeyPathUi} from "../Shared/ActionKeyPathUi";
import Radio from '@material-ui/core/Radio';
import RadioGroup from '@material-ui/core/RadioGroup';
import FormControlLabel from '@material-ui/core/FormControlLabel';
import ReactTable from "react-table";
import "react-table/react-table.css";
import {API_URL} from "../stores/DataSourceListener";
import moment from 'moment';

import IconButton from '@material-ui/core/IconButton';
import { ReactComponent as CopyIcon } from './CopyIcon.svg';

import Snackbar from '@material-ui/core/Snackbar';
import MuiAlert from '@material-ui/lab/Alert';
import { makeStyles } from '@material-ui/core/styles'
import copy from 'copy-to-clipboard';
import PropTypes from "prop-types";
import dataSourceStore from "../stores/DataSourceStore";

const useStyles2 = makeStyles((theme) => ({
    root: {
        '& > *': {
            margin: theme.spacing(1),
        },
    },
}));

export default class SessionsTable extends Component {
    static displayName = SessionsTable.name;

    static propTypes = {
        sessions: PropTypes.arrayOf(PropTypes.object),
    }

    constructor(props) {
        super(props);

        this.updateSessions = this.updateSessions.bind(this);
        this.handleVisibility = this.handleVisibility.bind(this);
    }

    updateSessions() {
        this.setState({sessions: sessionsStore.getSessions()});
    }

    handleVisibility(isVisible) {
        Helper.toConsole(`[${SessionsTable.name}] handleVisibility(${isVisible})`);
    }

    sessionsExample=`
[
  {
    "idSession": 0,
    "startedAt": "2023-02-20T02:23:01.447Z",
    "endedAt": "2023-02-20T02:23:01.447Z",
    "isFinished": true,
    "caption": "string",
    "maxDurationMinutes": 0
  }
]`;


    render() {
        const classes = useStyles2;
        
        const sessions = this.state.sessions ? this.state.sessions : []  
        
        const sessionsAsDebug = sessions.map((session,index) =>
            <li key={session.idSession.toString()}>
                #{session.idSession} '{session.caption}', {session.startAt} ... {session.endedAt}, stopped: {session.isFinished}
            </li>
        );
        
        return (
            <React.Fragment>
                <div className={classes.root}>
                    {sessionsAsDebug}
                </div>                
            </React.Fragment>
        )
    }
}
