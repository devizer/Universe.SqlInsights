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

import { withStyles } from '@material-ui/core/styles';
import { green } from '@material-ui/core/colors';
import { red } from '@material-ui/core/colors';

const ColorRadio = color => withStyles({
    root: {
        color: color[400],
        '&$checked': {
            color: color[600],
        },
    },
    checked: {},
})((props) => <Radio color="default" {...props} />);

const GreenRadio = ColorRadio(green);


const radiosExpire = [
    { label: 'for 1 hour', minutes: 60, color: green },
    { label: 'for 8 hours', minutes: 60*8, color: green },
    { label: 'for 24 hours', minutes: 60*24, color: green },
    { label: 'never expire', minutes: null, color: red },
];

export default class SessionsEditorDialog extends Component {
    static displayName = SessionsEditorDialog.name;

    static propTypes = {
        session: PropTypes.object.isRequired,
        titleMode: PropTypes.oneOf(['Edit', 'New', 'Delete']).isRequired,
        isOpened: PropTypes.bool.isRequired,
        onClose: PropTypes.func.isRequired,
    }

    constructor(props) {
        super(props);

        this.state = {
            session: this.props.session,
            isOpened: this.props.isOpened,
            selectedExpire: radiosExpire[radiosExpire.length - 1], 
        };
    }

    componentWillReceiveProps(nextProps, nextContext) {
        if (nextProps.session !== this.state.session
            || nextProps.titleMode !== this.state.titleMode
            || nextProps.isOpened !== this.state.isOpened
            || nextProps.onClose !== this.state.onClose
        ) {
            this.setState({
                session: nextProps.session,
                titleMode: nextProps.titleMode,
                isOpened: nextProps.isOpened,
                onClose: nextProps.onClose,
            });
        }
    }

    render() {

        const handleClose = () => {
            this.setState({isOpened: false});
            if (this.props.onClose) this.props.onClose(this.state.session);
        }
        
        
        const selectedExpire = this.state.selectedExpire; 
        
        return (
        <Dialog open={this.props.isOpened} onClose={handleClose} aria-labelledby="form-dialog-title" fullWidth={true} maxWidth="md">
            <DialogTitle id="form-dialog-title">{this.props.titleMode} Session</DialogTitle>
            <DialogContent>
                <TextField
                    autoFocus
                    margin="dense"
                    id="caption"
                    label="Caption"
                    type="text"
                    fullWidth
                    value={this.state.session?.Caption}
                />
                <DialogContentText className="center-aligned">
                    <br/>
                    {radiosExpire.map(radio => (
                        <>
                            &nbsp;
                            <GreenRadio
                                checked={selectedExpire?.label === radio.label}
                                onChange={() => this.setState({selectedExpire: radio}) }
                                value={radio.minutes}
                                name="radio-button-expire"
                                inputProps={{ 'aria-label': radio.label }}
                            />
                            {radio.label}
                            &nbsp;&nbsp;
                        </>
                    ))}
                </DialogContentText>
            </DialogContent>
            <DialogActions>
                <Button onClick={handleClose} color="primary">
                    Cancel
                </Button>
                <Button onClick={handleClose} color="primary">
                    Start
                </Button>
            </DialogActions>
        </Dialog>
        );
    }
}
