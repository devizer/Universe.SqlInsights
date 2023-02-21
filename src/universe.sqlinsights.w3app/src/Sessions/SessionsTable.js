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
// import { ReactComponent as CopyIcon } from './CopyIcon.svg';

import Snackbar from '@material-ui/core/Snackbar';
import MuiAlert from '@material-ui/lab/Alert';
import { makeStyles } from '@material-ui/core/styles'
import copy from 'copy-to-clipboard';
import PropTypes from "prop-types";
import * as DocumentVisibilityStore from "../stores/DocumentVisibilityStore";
import {SelectedSessionUpdated} from "../stores/SessionsActions";
import * as SessionsActions from "../stores/SessionsActions";

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

        this.state = {
            sessions: null,
            selectedSession: null,
            sorting: [{id: "Caption", desc: false}],
        };

    }

    componentDidMount()
    {
        let x = sessionsStore.on('storeUpdated', this.updateSessions);
        DocumentVisibilityStore.on(this.handleVisibility);
    }

    componentWillUnmount() {
        sessionsStore.off('storeUpdated', this.updateSessions);
    }
    
    updateSessions(arg) {
        // console.log(`%c updateSessions `, 'color: darkred', arg.IdSession)
        let sessions = sessionsStore.getSessions();
        if (sessions)
            this.setState({sessions: sessions});
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
        const isLoaded = this.state.sessions !== null;
        const pageSize = sessions.length === 0 ? 1 : Math.max(sessions.length, 1);
        
        const onSortedChange = (newSorted, column, shiftKey) => {
            console.log('%c NEW SESSIONS SORTING', 'background: #222; color: #bada55', newSorted);
            const id = newSorted[0].id;
            const newSorting = [{ id: id, desc: newSorted[0].desc }];
            this.setState({sorting:newSorting});
        };

        const selectedRowHandler = (state, rowInfo, column) => {
            if (rowInfo && rowInfo.row) {
                const isSelected = this.state.selectedSession && rowInfo.original.IdSession === this.state.selectedSession.IdSession;
                return {
                    onClick: (e) => {
                        const selectedRow = rowInfo.original;
                        this.setState({
                            selectedSession: selectedRow, 
                            selectedIndex: rowInfo.index,
                        });
                        SessionsActions.SelectedSessionUpdated(selectedRow);
                        Helper.toConsole("Session Selected", selectedRow);
                        if (this.props.onActionSelected)
                            this.props.onActionSelected(selectedRow);
                    },
                    style: {
                        background: isSelected ? '#4f9a94' : 'white',
                        color: isSelected ? 'white' : 'black',
                        cursor: "pointer",
                    }
                }
            } else {
                return {}
            }
        }

        const noDataProps = {style:{color:"gray", marginTop:28, padding: 1, border: "1px solid transparent"}};

        const cellCaption = row => row.original.Caption;
        const dateColumnWidth = 220;
        const parseMyDate = arg => arg ? (arg instanceof Date ? arg : new Date(arg)) : null;

        let today = new Date();
        const cellDate = propertyName => row =>  {
            // return JSON.stringify(row.original);
            const at = parseMyDate(row.original[propertyName]);
            if (!at) return null;
            const atDay = new Date(at.getTime()); atDay.setHours(0,0,0,0);
            const mom = moment(at);
            if (today.getTime() === atDay.getTime()) return mom.format("LTS"); else return mom.format("ll, LTS");
        };

        return (
            <React.Fragment>

                <ReactTable
                    data={sessions}
                    sorted={this.state.sorting}
                    onSortedChange={onSortedChange}
                    getTrProps={selectedRowHandler}
                    showPagination={false}
                    defaultPageSize={pageSize}
                    pageSizeOptions={[pageSize]}
                    pageSize={pageSize}
                    noDataText={isLoaded ? "no any sessions" : "waiting for cells"}
                    getNoDataProps={() => noDataProps}
                    className="-striped -highlight"
                    columns={
                        [
                            {
                                Header: "Session",
                                accessor: "Caption",
                                minWidth: 540,
                                Cell: cellCaption,
                            },
                            {
                                Header: "Started At",
                                accessor: x => parseMyDate(x.StartedAt),
                                id: "StartedAt",
                                width: dateColumnWidth,
                                Cell: cellDate("StartedAt"),
                            },
                            {
                                Header: "Ended(ing) At",
                                accessor: x => parseMyDate(x.CalculatedEnding),
                                id: "EndedAt",
                                width: dateColumnWidth,
                                Cell: cellDate("CalculatedEnding"),
                            },
                        ]
                    }
                />

            </React.Fragment>
        )
    }
}
