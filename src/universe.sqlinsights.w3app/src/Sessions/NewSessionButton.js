import * as Helper from "../Helper"
import React, { Component } from 'react';
import Button from '@material-ui/core/Button';
import SaveIcon from '@material-ui/icons/Save';
import SlideshowIcon from '@material-ui/icons/Slideshow';
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

import Snackbar from '@material-ui/core/Snackbar';
import MuiAlert from '@material-ui/lab/Alert';
import { makeStyles } from '@material-ui/core/styles'
import copy from 'copy-to-clipboard';
import PropTypes from "prop-types";

const useStyles = makeStyles((theme) => ({
    button: {
        margin: theme.spacing(1),
    },
}));

export default class NewSessionButton extends Component {
    static displayName = NewSessionButton.name;

    static propTypes = {
        onClick: PropTypes.func.isRequired,
    }

    constructor(props) {
        super(props);
    }

    render() {

        const classes = useStyles;
        
        return (
            <React.Fragment>
                <Button
                    id="new-session-button"
                    variant="contained"
                    color="primary"
                    size="large"
                    className={classes.button}
                    startIcon={<SlideshowIcon />}
                    onClick={this.props.onClick} 
                >
                    New Session
                </Button>
            </React.Fragment>
        )



    }
}
