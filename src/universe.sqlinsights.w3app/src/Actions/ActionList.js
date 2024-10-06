import * as Helper from "../Helper"
import { DynamicDownloading } from '../DynamicDownloading'
import React, { Component } from 'react';
import dataSourceStore from "../stores/DataSourceStore";
import {ActionKeyPathUi} from "../Shared/ActionKeyPathUi";
import ReactTable from "react-table";
import "react-table/react-table.css";
import {API_URL} from '../BuildTimeConfiguration';
import moment from 'moment';

import IconButton from '@material-ui/core/IconButton';
import { ReactComponent as CopyIcon } from './CopyIcon.svg';
import { ReactComponent as DownloadIcon } from './Download.svg';

import Snackbar from '@material-ui/core/Snackbar';
import MuiAlert from '@material-ui/lab/Alert';
import { makeStyles } from '@material-ui/core/styles'
import copy from 'copy-to-clipboard';

import SyntaxHighlighter from 'react-syntax-highlighter';
import { docco } from 'react-syntax-highlighter/dist/esm/styles/hljs';
import PropTypes from "prop-types";
import sessionsStore from "../stores/SessionsStore"
import settingsStore from "../stores/SettingsStore";
import ReactComponentWithPerformance from "../Shared/ReactComponentWithPerformance";

let renderCount = 0;
const SqlCode = ({codeString}) => {
    return (
        <SyntaxHighlighter language="sql" style={docco} wrapLines={true} wrapLongLines={true} /*CodeTag={"span"} PreTag={"span"}*/>
            {codeString}
        </SyntaxHighlighter>
    );
};
// import * as DataSourceActions from "../stores/DataSourceActions";

function Alert(props) {
    return <MuiAlert elevation={6} variant="filled" {...props} />;
}

const useStyles = makeStyles((theme) => ({
    root: {
        width: '100%',
        '& > * + *': {
            marginTop: theme.spacing(2),
        },
    },
}));

const noDataProps = {style:{color:"gray", marginTop:30, border: "1px solid grey"}};

export default class ActionList extends ReactComponentWithPerformance {
    static displayName = ActionList.name;
    internalName = () => "ActionList";

    static propTypes = {
        keyPath: PropTypes.arrayOf(PropTypes.string),
    }

    constructor(props) {
        super(props);
        
        this.updateTick = this.updateTick.bind(this);
        this.checkActionsTimestamp = this.checkActionsTimestamp.bind(this);
        
        Helper.toConsole("ActionList.CTOR {props.keyPath}", props.keyPath);
        
        this.state = {
            actions: null,
            openedCopyConfirmation: false,
            actionsTimestamp: null,
        };
    }

    componentDidMount()
    {
        this.timerId = setInterval(this.updateTick, 1000);
    }

    componentWillUnmount() {
        if (this.timerId)
            clearInterval(this.timerId);
        
        this.timerId = null;
    }

    // https://stackoverflow.com/questions/32414308/updating-state-on-props-change-in-react-form
    componentWillReceiveProps(nextProps) {
        Helper.toConsole(`On Selected Key Path Updated [${this.props.keyPath}]`, this.props.keyPath);
        if (`${nextProps.keyPath}` !== `${this.props.keyPath}`) {
            this.setState({ actions: null, actionsTimestamp: null });
            // setTimeout(this.updateTick, 0);
            setTimeout(this.checkActionsTimestamp, 0);
        }
    }
    
    updateTick() {
        if (settingsStore.getAutoUpdateSummary()) {
            this.checkActionsTimestamp();
        }
    }

    checkActionsTimestamp() {
        Helper.toConsole("Requesting TIMESTAMP of", this.props.keyPath);
        if (!this.props.keyPath) return;
        
        // const apiTimestamp=`${API_URL}/ActionsTimestamp?key=${this.props.keyPath.join(Helper.keyPathSeparator.char)}`;
        const apiTimestamp=`${API_URL}/ActionsTimestamp`;
        try {
            const idSession = sessionsStore.getSelectedSession()?.IdSession ?? -1;
            const body = Helper.populateAppAndHostFiltersOfBody({Path: this.props.keyPath, IdSession: idSession});
            const req = Helper.createRequest('ActionsTimestamp', body)
            fetch(req)
                .then(response => {
                    return response.ok ? response.json() : {error: response.status, details: response.json()}
                })
                .then(actionsTimestamp => {
                    const prevTimestamp = this.state.actionsTimestamp;
                    const isChanged = actionsTimestamp !== prevTimestamp;  
                    Helper.toConsole(`Obtained actionsTimestamp [${(isChanged ? "CHNAGED" : "SAME")}] Of Group '${ActionKeyPathUi({path: this.props.keyPath})}'`, actionsTimestamp);
                    if (isChanged)
                        this.updateList(actionsTimestamp);
                })
                .catch(error => {
                    console.error(error);
                });
        } catch (err) {
            console.error(`FETCH failed for ${apiTimestamp}. ${err}`);
        }
    }

    updateList(newTimestamp) {
        Helper.toConsole(`Requesting ACTION LIST (TS is ${newTimestamp}) of`, this.props.keyPath);
        if (!this.props.keyPath) return;

        // const apiUrl=`${API_URL}/ActionsByKey?key=${this.props.keyPath.join(Helper.keyPathSeparator.char)}`;
        const apiUrl=`${API_URL}/ActionsByKey`;
        try {
            const idSession = sessionsStore.getSelectedSession()?.IdSession ?? -1;
            const body = Helper.populateAppAndHostFiltersOfBody({Path: this.props.keyPath, IdSession: idSession});
            const req = Helper.createRequest('ActionsByKey', body);
            fetch(req)
                .then(response => {
                    // console.log(`Response.Status for ${apiUrl} obtained: ${response.status}`);
                    // console.log(response);
                    // console.log(response.body);
                    return response.ok ? response.json() : {error: response.status, details: response.json()}
                })
                .then(detailsOfGroup => {
                    Helper.toConsole(`Obtained ACTION LIST Of Group '${ActionKeyPathUi({path: this.props.keyPath})}'`, detailsOfGroup);
                    // detailsOfGroup.reverse();
                    this.setState((state,props) => ({
                        actions: detailsOfGroup,
                        actionsTimestamp: newTimestamp ? newTimestamp : state.actionsTimestamp
                    }));
                })
                .catch(error => {
                    console.error(error);
                });
        } catch (err) {
            console.error(`FETCH failed for ${apiUrl}. ${err}`);
        }
    }

    render() {
        Helper.toConsole(`${++renderCount} Rendering «Actions»`);

        const actions = this.state.actions === null ? [] : this.state.actions;
        const isLoaded = this.state.actions !== null;
        let pageSize = actions.length === 0 ? 9 : Math.max(actions.length, 1);
        
        // AT Column
        // const parseMyDate = arg => new Date(parseInt(arg.substr(6))); // microsoft
        const parseMyDate = arg => new Date(arg);
        
        let today = new Date(); 
        today.setHours(0,0,0,0);
        const cellAt = row =>  {
            const at = parseMyDate(row.original.At);
            const atDay = new Date(at.getTime()); atDay.setHours(0,0,0,0);
            const mom = moment(at);
            if (today.getTime() === atDay.getTime()) return mom.format("LTS"); else return mom.format("LTS, ll"); 
        };
        
        const onDownload = actionDetails => e => {
            const newLine = "\r\n";
            const countersToString = counters => {
                const copy = {...counters};
                copy.Requests = undefined;
                return copy;
            };
            console.log("ActionList.onDownload argument (action object)", actionDetails);
            const keyPath = ActionKeyPathUi({path:actionDetails.Key.Path})
            const sqlStatements = actionDetails.SqlStatements;
            let text = `/* Action: ${keyPath} */${newLine}`;
            text = text + sqlStatements.map(s => `${newLine}--- ${JSON.stringify(countersToString(s.Counters))} ---${newLine}${s.Sql}`).join(newLine) + newLine;
            DynamicDownloading(text, 'text/plain', `${keyPath}.sql`);
        };
        
        const onCopy = actionDetails => e => {
            const newLine = "\r\n";
            const countersToString = counters => {
                const copy = {...counters};
                copy.Requests = undefined;
                return copy;
            };
            console.log("ActionList.onCopy argument (action object)", actionDetails);
            const keyPath = ActionKeyPathUi({path:actionDetails.Key.Path})
            const sqlStatements = actionDetails.SqlStatements;
            let text = `/* Action: ${keyPath} */${newLine}`;
            text = text + sqlStatements.map(s => `${newLine}--- ${JSON.stringify(countersToString(s.Counters))} ---${newLine}${s.Sql}`).join(newLine) + newLine;
            copy(text, {format: "text/plain"});
            this.setState({openedCopyConfirmation:true});
        };
        
        // SQL Column
        const cellSql = row => {
            const statements = row.original.SqlStatements;
            if (!statements || statements.length === 0) return "";
            const maxVisibleStatementsCount = 42;
            let isTrimmed = statements.length > maxVisibleStatementsCount - 1;
            const visibleStatements = isTrimmed ? statements.slice(0, maxVisibleStatementsCount - 1) : statements;
            
            return (
                <>
                <table className="SqlStatements" style={{width:'100%'}}>
                    <thead>
                    <tr>
                        <th className="sql-error center-aligned">Error</th>
                        <th className="sql-code">
                            Code • {statements.length} statement{statements.length > 1 ? "s" : ""}
                            &nbsp;&nbsp;
                            <IconButton onClick={onCopy(row.original)}><CopyIcon style={{width:20,height:20,marginLeft:2,marginRight:2,paddingTop:2, opacity:0.9}}/></IconButton> 
                            &nbsp;
                            <IconButton onClick={onDownload(row.original)} style={{marginLeft:-12}}><DownloadIcon style={{width:20,height:20,marginLeft:2,marginRight:2,paddingTop:-1,marginTop:0, opacity:0.9}}/></IconButton>
                        </th>
                        <th className="sql-cpu">CPU, ms</th>
                        <th className="sql-io">I/O, pages</th>
                    </tr>
                    </thead>
                    <tbody>
                    {visibleStatements.map((statement, index) =>
                        <tr key={index} className={statement.SqlErrorCode ? "damn" : ""}>
                            <td className="sql-error">{statement.SqlErrorCode}</td>
                            <td className="sql-code"><SqlCode codeString={statement.Sql} /></td>
                            <td className="sql-cpu center-aligned">{statement.Counters.CPU.toLocaleString()}<small>&nbsp;of&nbsp;</small>{statement.Counters.Duration.toLocaleString()}</td>
                            <td className="sql-io center-aligned">
                                {statement.Counters.Reads.toLocaleString()}<small>&nbsp;/&nbsp;</small>{statement.Counters.Writes.toLocaleString()}
                                {statement.Counters?.RowCounts > 0 && <>
                                    <hr className="hr-rows-separator" />
                                    {statement.Counters?.RowCounts}
                                    <small style={{paddingLeft:4}}>
                                        {statement.Counters?.RowCounts > 1 ? "rows" : "row"}
                                    </small>
                                </>
                                }
                            </td>
                        </tr>
                    )}
                    </tbody>
                </table>
                
                {isTrimmed && <div className="center-aligned"> 
                    {statements.length - visibleStatements.length} statements are hidden. Click <i>copy</i> button:
                    <IconButton onClick={onCopy(row.original)}><CopyIcon style={{width:20,height:20,marginLeft:2,marginRight:2,paddingTop:2, opacity:0.9}}/></IconButton>
                </div>}
                </>
            );
        };

        const formatNumber = time => {
            let ret = time.toLocaleString(undefined, { minimumFractionDigits: 0, maximumFractionDigits: time < 100 ? 2 : (time < 1000 ? 1 : 0) });
            return ret === "0" ? "" : ret;
        };

        const handleCloseCopyConfirmation = (event, reason) => {
            if (reason === 'clickaway') {
                return;
            }
            
            this.setState({openedCopyConfirmation:false});
        };

        const cellNumber = accessor => row => (<span>{formatNumber(accessor(row.original))}</span>);

        return (
            <React.Fragment>
                {this.props.keyPath && <h3 className="ActionDetailsHeader center-aligned padding-top">
                    Latest Actions for “<b><ActionKeyPathUi path={this.props.keyPath} /></b>”
                    {(!isLoaded) && ", loading"}
                    {(actions && isLoaded && actions.length === 1) && ", 1 action"}
                    {(actions && isLoaded && actions.length > 1) && `, ${actions.length} actions`}
                </h3>}
                {isLoaded && <ReactTable
                    data={actions}
                    showPagination={false}
                    defaultPageSize={pageSize}
                    pageSizeOptions={[pageSize]}
                    pageSize={pageSize}
                    noDataText={isLoaded ? "no actions triggered" : "waiting for cells"}
                    getNoDataProps={() => noDataProps}
                    className="-striped -highlight"
                    columns={[
                        {
                            Header: "App",
                            accessor: x => `${x.AppName}@${x.HostId}`,
                            id: 'App'
                        },
                        {
                            Header: "At",
                            accessor: x => parseMyDate(x.At).toString(),
                            id: "At",
                            Cell: cellAt,
                        },
                        {
                            Header: "Brief Exception",
                            accessor: "BriefException",
                            className: 'multiline-cell',
                            minWidth: 250,
                        },
                        {
                            Header: "App Duration",
                            accessor: "AppDuration",
                            width: 85,
                            className: "right-aligned",
                            Cell: cellNumber(x => x.AppDuration),
                        },
                        {
                            Header: "App CPU",
                            accessor: x => x.AppKernelUsage + x.AppUserUsage,
                            id: "AppCpu",
                            width: 85,
                            className: "right-aligned",
                            Cell: cellNumber(x => x.AppKernelUsage + x.AppUserUsage)
                        },
                        {
                            Header: "App Kernel CPU",
                            accessor: "AppKernelUsage",
                            width: 80,
                            className: "right-aligned",
                            Cell: cellNumber(x => x.AppKernelUsage)
                        },
                        {
                            Header: "App User CPU",
                            accessor: "AppUserUsage",
                            width: 85,
                            className: "right-aligned",
                            Cell: cellNumber(x => x.AppUserUsage)
                        },
                        {
                            Header: "SQL Side",
                            accessor: "SqlStatements",
                            className: 'multiline-cell',
                            minWidth: 810,
                            Cell: cellSql,
                        },
                    ]}
                />}

                <Snackbar open={this.state.openedCopyConfirmation} autoHideDuration={3000} onClose={handleCloseCopyConfirmation}>
                    <Alert onClose={handleCloseCopyConfirmation} severity="success">
                        <span>The SQL code copied to clipboard!</span>
                    </Alert>
                </Snackbar>

            </React.Fragment>
        );
    }

}
        
