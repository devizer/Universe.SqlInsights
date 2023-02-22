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
import NewSessionButton from './NewSessionButton'
import SessionEditorDialog from "./SessionEditorDialog"

import { ReactComponent as MenuIconSvg } from './Menu-Icon-v2.svg';
import {ReactComponent as CopyIcon} from "../Actions/CopyIcon.svg";

import Menu from '@material-ui/core/Menu';
import MenuItem from '@material-ui/core/MenuItem';
import MoreVertIcon from '@material-ui/icons/MoreVert';
import Typography from '@material-ui/core/Typography';
import * as SessionIcons from './SvgIcons/Icons';
// import {IconRename} from './SvgIcons/Icons';

const MenuIcon = (size=10,color='#333') => (<MenuIconSvg style={{width: size,height:size,fill:color,strokeWidth:'1px',stroke:color }} />);



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
        // sessions: PropTypes.arrayOf(PropTypes.object),
    }

    constructor(props) {
        super(props);

        this.updateSessions = this.updateSessions.bind(this);
        this.handleVisibility = this.handleVisibility.bind(this);
        this.handleCloseEditor = this.handleCloseEditor.bind(this);

        this.state = {
            sessions: null,
            selectedSession: null,
            sorting: [{id: "Caption", desc: false}],
            isEditorOpened: false,
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
    
    handleCloseEditor() {
        this.setState({isEditorOpened: false});
    }

    sessionsExample=`
[
  {
    "IdSession": 0,
    "StartedAt": "2023-02-20T02:23:01.447Z",
    "EndedAt": "2023-02-20T02:23:01.447Z",
    "IsFinished": true,
    "Caption": "string",
    "MaxDurationMinutes": 0
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

        const selectedRowHandler = (state, rowInfo, instance) => {
            if (rowInfo && rowInfo.row) {
                const isSelected = this.state.selectedSession && rowInfo.original.IdSession === this.state.selectedSession.IdSession;
                // console.warn("STATE IS ", state);
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
                        fill: isSelected ? 'white' : 'black',
                        stroke: isSelected ? 'white' : 'black',
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
        
        const handleNewSessionClick = () => {
            this.setState({isEditorOpened: true});
        }

        const sessionsTemp = this.state.sessions;
        const newSessionCaption = `New Session ${1 + (sessionsTemp ? sessionsTemp.length : 0)}`;
        // console.log(`%c newSessionCaption="${newSessionCaption}"`, 'color: darkred');
        
        // const cellMenu = <IconButton onClick={() => {}}>{MenuIcon()}</IconButton>;

        const handleCloseSessionMenu = () => {
            this.setState({
                isSessionMenuOpened: false,
                sessionOfMenu: null,
                sessionMenuAnchor: null,
            });
        };
        
        const handleOpenSessionMenu = session => (event) => {
            const anchor = event.currentTarget;
            console.log(`%c OPENING SESSION ${session.Caption} MENU`, "color: darkred; background-color: #91FFB2");
            this.setState({
                isSessionMenuOpened:true, 
                sessionOfMenu: session,
                sessionMenuAnchor: anchor,
            });
        };

        const handleClickSessionMenu = menuOption => (event) => {
            event.stopPropagation();
            event.preventDefault();
            const sessionOfMenu = this.state.sessionOfMenu;
            console.warn(`%c CLICKED '${menuOption.title}' for session '${sessionOfMenu.Caption}'`);
            this.setState({
                isSessionMenuOpened: false,
                sessionOfMenu: null,
                sessionMenuAnchor: null,
            });
        };


        const cellMenu = row => (
            <>
                <IconButton size={"small"} onClick={handleOpenSessionMenu(row.original)}>
                    <MenuIconSvg style={{width:18,height:18,marginLeft:2,marginRight:2,paddingTop:0, opacity:0.6}}/>
                </IconButton>
                <span id={`menu-session-${row.original.IdSession}`}></span>
            </>
        );
        // const cellMenu = "";
        
        let sessionMenuOptions = [];
        const isStopped = Boolean(this.state.sessionOfMenu?.IsFinished);
        sessionMenuOptions.push({ title: "Rename", icon: SessionIcons.IconRename() });
        sessionMenuOptions.push({ title: "Delete", icon: SessionIcons.IconDelete() });
        if (isStopped) sessionMenuOptions.push({ title: "Resume", icon: SessionIcons.IconResume() });
        if (!isStopped) sessionMenuOptions.push({ title: "Stop", icon: SessionIcons.IconStop() });

        return (
            <React.Fragment>

                <div class="right-aligned">
                    <NewSessionButton onClick={handleNewSessionClick} />
                </div>
                
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
                                Header: " ",
                                accessor: "IdSession",
                                Cell: cellMenu,
                                minWidth: 46,
                                width: 46,
                                className: 'center-aligned',
                                sortable: false,
                            },
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

                <Menu
                    id="long-menu"
                    /*anchorEl={this.state.sessionMenuAnchor}*/
                    anchorEl={() => document.getElementById(`menu-session-${this.state.sessionOfMenu?.IdSession}`)}
                    keepMounted
                    open={this.state.isSessionMenuOpened}
                    onClose={handleCloseSessionMenu}
                    PaperProps={{
                        style: {
                            maxHeight: 300,
                            width: 250,
                            minWidth: 250,
                        },
                    }}
                >
                    <Typography variant="caption" display="block" gutterBottom noWrap className={"center-aligned"}>
                        session
                    </Typography>
                    <Typography variant="button" display="block" gutterBottom noWrap className={"center-aligned"} style={{paddingLeft:16, paddingRight:16}}>
                        {this.state.sessionOfMenu?.Caption}
                    </Typography>
                    <hr/>

                    {sessionMenuOptions.map((option) => (
                        <MenuItem key={option.title} selected={false} onClick={handleClickSessionMenu(option)}>
                            {/*{MenuIcon(15,'#555')}&nbsp;&nbsp;*/}
                            {/*<IconRename size={20} color={"$555"} />*/}
                            {option.icon}&nbsp;&nbsp;
                            {option.title}
                        </MenuItem>
                    ))}
                </Menu>
                
                <SessionEditorDialog session={{Caption: newSessionCaption}} isOpened={this.state.isEditorOpened} onClose={this.handleCloseEditor} titleMode={"New"} />

            </React.Fragment>
        )
    }
}
