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

import Snackbar from '@material-ui/core/Snackbar';
import MuiAlert from '@material-ui/lab/Alert';
import { makeStyles } from '@material-ui/core/styles'
import copy from 'copy-to-clipboard';
import PropTypes from "prop-types";
import * as DocumentVisibilityStore from "../stores/DocumentVisibilityStore";
import {SelectedSessionUpdated} from "../stores/SessionsActions";

import { Dialog } from '@material-ui/core';
import Button from '@material-ui/core/Button';
import TextField from '@material-ui/core/TextField';
import DialogActions from '@material-ui/core/DialogActions';
import DialogContent from '@material-ui/core/DialogContent';
import DialogContentText from '@material-ui/core/DialogContentText';
import DialogTitle from '@material-ui/core/DialogTitle';


export default class SessionsEditorDialog extends Component {
    static displayName = SessionsEditorDialog.name;

    static propTypes = {
        session: PropTypes.arrayOf(PropTypes.object),
        isOpened: PropTypes.arrayOf(PropTypes.bool),
    }

    constructor(props) {
        super(props);

        this.state = {
            session: this.props.session,
            isOpened: this.props.isOpened,
        };

    }
    
    render() {
        
        return <Dialog open={this.state.isOpened} />  
        
    }


}
