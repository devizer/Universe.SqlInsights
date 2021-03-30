import * as Helper from "../Helper"
import React, { Component } from 'react';
import dataSourceStore from "../stores/DataSourceStore";
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
    }
    
    render() {
        const classes = useStyles2;

        return (
            <React.Fragment>
                <div className={classes.root}>
                </div>                
            </React.Fragment>
        )
    }
}
