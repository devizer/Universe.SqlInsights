import * as Helper from "../Helper"
import React, { Component } from 'react';
import sessionsStore from "../stores/SessionsStore";
import {ActionKeyPathUi} from "../Shared/ActionKeyPathUi";
import Radio from '@material-ui/core/Radio';
import RadioGroup from '@material-ui/core/RadioGroup';
import FormControlLabel from '@material-ui/core/FormControlLabel';
import ReactTable from "react-table";
import "react-table/react-table.css";
// import {API_URL} from "../stores/DataSourceListener";
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
import Typography from "@material-ui/core/Typography";

const ColorRadio = color => withStyles({
    root: {
        color: color[900],
        '&$checked': {
            color: color[999],
        },
    },
    checked: {},
})((props) => <Radio color="default" {...props} />);

const GreenRadio = ColorRadio(green);


const expireOptions = [
    { label: 'for 1 hour', minutes: 60, color: green },
    { label: 'for 8 hours', minutes: 60*8, color: green },
    { label: 'for 24 hours', minutes: 60*24, color: green },
    { label: 'never expire', minutes: null, color: red },
];
const defaultExpireOptions = expireOptions[0]; 

export default class SessionsEditorDialog extends Component {
    static displayName = SessionsEditorDialog.name;

    static propTypes = {
        session: PropTypes.object.isRequired,
        titleMode: PropTypes.any.isRequired,
        isOpened: PropTypes.bool.isRequired,
        onClose: PropTypes.func.isRequired,
        buttons: PropTypes.array.isRequired,
        visibleEditors: PropTypes.array.isRequired,
    }

    constructor(props) {
        super(props);

        this.state = {
            session: this.props.session,
            isOpened: this.props.isOpened,
            selectedExpire: defaultExpireOptions,
            buttons: this.props.buttons,
            titleMode: this.props.titleMode,
            visibleEditors: this.props.visibleEditors,
        };
    }

    componentWillReceiveProps(nextProps, nextContext) {
        if (nextProps.session !== this.state.session
            || nextProps.titleMode !== this.state.titleMode
            || nextProps.isOpened !== this.state.isOpened
            || nextProps.onClose !== this.state.onClose
            || nextProps.buttons !== this.state.buttons
            || nextProps.visibleEditors !== this.state.visibleEditors
        ) {
            const session = {...(nextProps.session ?? {}), MaxDurationMinutes: defaultExpireOptions.minutes };
            this.setState({
                session: session,
                titleMode: nextProps.titleMode,
                isOpened: nextProps.isOpened,
                onClose: nextProps.onClose,
                buttons: nextProps.buttons,
                selectedExpire: defaultExpireOptions,
                visibleEditors: nextProps.visibleEditors,
            });
        }
    }

    render() {

        const handleClose = () => {
            this.setState({isOpened: false});
            if (this.props.onClose) this.props.onClose(this.state.session);
        }
        const handleButton = button =>() => {
            this.setState({isOpened: false});
            if (this.props.onClose) this.props.onClose(this.state.session, button);
        }

        const selectedExpire = this.state.selectedExpire;

        const buttonFakeCancel = {caption: "Resume", variant: "contained", color: "primary", action: () => console.log("%c Dialog canceled", "background-color: #5998FF")};
        
        const handleRadioExpire = expireOption => e => {
            this.state.session.MaxDurationMinutes = expireOption.minutes; // TODO: Fix?
            this.setState({
                selectedExpire: expireOption,
                session: { ...this.state.session, MaxDurationMinutes: expireOption.minutes }
            });
        }

        const visibleCaptionEditor = Boolean((this.state.visibleEditors ?? []).find(x => x === "CaptionEditor"));
        const visibleMaxDurationMinutesEditor = Boolean((this.state.visibleEditors ?? []).find(x => x === "MaxDurationMinutesEditor"));
        
        const handleCaptionKey = event => {
            console.log("Caption Key Down:", event);
            if (event.code === "Enter" || event.code === "NumpadEnter") {
                if (this.props.buttons?.length > 0) {
                    const lastButton = this.props.buttons[this.props.buttons.length - 1];
                    console.warn("LAST BUTTON TRIGGER", lastButton);
                    handleButton(lastButton);
                }
            }
        }
        
        
        return (
        <Dialog open={this.props.isOpened} onClose={handleClose} aria-labelledby="form-dialog-title" fullWidth={true} maxWidth="sm">
            <DialogTitle id="form-dialog-title">{this.props.titleMode} “{this.state.session?.Caption}” </DialogTitle>
            <DialogContent>
                {visibleCaptionEditor &&
                    <TextField
                        autoFocus
                        margin="dense"
                        id="caption"
                        label="Caption"
                        type="text"
                        fullWidth
                        onKeyDown={handleCaptionKey}
                        value={this.state.session?.Caption}
                        onChange={e => {
                            const session = this.state.session ?? {};
                            session.Caption = e.target.value;
                            this.setState({session})
                        }}
                    />
                }
                {visibleMaxDurationMinutesEditor &&
                    <DialogContentText className="center-aligned" style={{color: "black"}}>
                        <br/>
                        {expireOptions.map(expireOption => (
                            <>
                                &nbsp;
                                <GreenRadio
                                    checked={selectedExpire?.label === expireOption.label}
                                    onChange={handleRadioExpire(expireOption)}
                                    value={expireOption.minutes}
                                    name="radio-button-expire"
                                    inputProps={{'aria-label': expireOption.label}}
                                />
                                {expireOption.label}
                                &nbsp;
                            </>
                        ))}
                    </DialogContentText>
                }
            </DialogContent>
            <DialogActions>
                <Typography variant="caption" display="block" gutterBottom noWrap className={"right-aligned"} style={{padding: 16}}>

                    <Button onClick={handleButton(buttonFakeCancel)} variant="text" color="primary">
                        Cancel
                    </Button>

                    {this.props.buttons.map((button,index) => (
                        <>
                            &nbsp;&nbsp;&nbsp;&nbsp;
                            <Button onClick={handleButton(button)} variant={button.variant} color={button.color}>
                                {button.caption}
                            </Button>
                        </>
                    ))}
                    
                </Typography>

{/*
                <Button onClick={handleButton("delete")}  variant="contained" color="secondary">
                    Delete
                </Button>
                <Button onClick={handleButton("start")}  variant="contained" color="primary">
                    Start
                </Button>
*/}
            </DialogActions>
        </Dialog>
        );
    }
}
