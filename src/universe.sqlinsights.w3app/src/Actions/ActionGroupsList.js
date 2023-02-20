import * as Helper from "../Helper"
import React, { Component } from 'react';
import PropTypes from 'prop-types';
import Radio from '@material-ui/core/Radio';
import RadioGroup from '@material-ui/core/RadioGroup';
import FormControlLabel from '@material-ui/core/FormControlLabel';
import ReactTable from "react-table";
import "react-table/react-table.css";

import dataSourceStore from "../stores/DataSourceStore";
import {ActionKeyPathUi} from "../Shared/ActionKeyPathUi";
import ActionList from "./ActionList";
import * as DocumentVisibilityStore from "../stores/DocumentVisibilityStore";
import sessionsStore from "../stores/SessionsStore";

const noDataProps = {style:{color:"gray", marginTop:30, border: "1px solid grey"}};

export default class ActionGroupsList extends Component {
    static displayName = ActionGroupsList.name;

    static propTypes = {
        onActionSelected: PropTypes.func
    }

    constructor(props) {
        super(props);
        
        this.updateDataSource = this.updateDataSource.bind(this);
        this.handleVisibility = this.handleVisibility.bind(this);

        this.state = {
            actions: null,
            selected: null,
            selectedRow: null,
            kind: 'total', // or 'average'
            sorting: [{id: "AppDuration", desc: true}],
        };
    }
    
    handleVisibility(isVisible) {
        Helper.toConsole(`handleVisibility(${isVisible})`);
    }

    componentDidMount()
    {
        let x = dataSourceStore.on('storeUpdated', this.updateDataSource);
        DocumentVisibilityStore.on(this.handleVisibility);
    }

    componentWillUnmount() {
        dataSourceStore.off('storeUpdated', this.updateDataSource);
    }

    updateDataSource() {
        this.setState({actions: dataSourceStore.getDataSource()});
    }
    
    render() {
        const isLoaded = this.state.actions !== null;
        const actions = this.state.actions === null ? [] : this.state.actions;
        Helper.toConsole(`Rendering TOTAL ACTION GROUPS`, actions.length);
        const handleChangeKind = (event) => {
            this.setState({kind: event.target.value})
        };

        const onSortedChange = (newSorted, column, shiftKey) => {
            // const defaultSorting = [{ id: 'totalCpuUsage_PerCents', desc: true }]
            const id = newSorted[0].id;
            const descDirection = id !== "KeyString";
            const newSorting = [{ id: id, desc: descDirection }];
            this.setState({sorting:newSorting});
        };

        let pageSize = actions.length === 0 ? 9 : Math.max(actions.length, 1);
        
        // const cellKeyPath = row => (<span><ActionKeyPathUi path={row.original.Key.Path} /></span>);
        const cellKeyPath = row => row.original.KeyString;
        
        const formatNumber = time => {
            let ret = time.toLocaleString(undefined, { minimumFractionDigits: 0, maximumFractionDigits: time < 0 ? 0 : time < 100 ? 2 : (time < 1000 ? 1 : 0) });
            return ret === "0" ? "" : ret;
        };
        
        const isAverage = this.state.kind === "average";
        const getCellValue = (rowData, accessor) => {
            let total = accessor(rowData);
            return isAverage ? total / rowData.Count : total;
        };
        const cellNumber = accessor => row => (<span>{formatNumber(getCellValue(row.original, accessor))}</span>);
        const cellTotalErrors = row => formatNumber(row.original.RequestErrors);
        

        const selectedRowHandler = (state, rowInfo, column) => {
            if (rowInfo && rowInfo.row) {
                return {
                    onClick: (e) => {
                        const selectedRow = rowInfo.original;
                        this.setState({
                            selected: rowInfo.index,
                            selectedRow: selectedRow,
                        });
                        Helper.toConsole("Action Selected", selectedRow);
                        if (this.props.onActionSelected)
                            this.props.onActionSelected(selectedRow);
                    },
                    style: {
                        background: rowInfo.index === this.state.selected ? '#4f9a94' : 'white',
                        color: rowInfo.index === this.state.selected ? 'white' : 'black',
                        cursor: "pointer",
                    }
                }
            } else {
                return {}
            }
        }
        
        const defaultMetricColumnWidth = 90;
        let noDataText = isLoaded ? "no actions triggered" : "waiting for cells";
        if (!sessionsStore.getSelectedSession()) noDataText = "select a session";
            
        return (
            <React.Fragment>
                    <RadioGroup row aria-label="kind" name="kind" value={this.state.kind} onChange={handleChangeKind} className='center-aligned'>
                        <div style={{textAlign: 'center', width: '100%'}}>
                        <FormControlLabel control={<null />} label="Display:" />
                        <FormControlLabel value="average" control={<Radio />} label="Average" />
                        <FormControlLabel value="total" control={<Radio />} label="Total" />
                        </div>
                    </RadioGroup>
                <ReactTable
                    data={actions}
                    sorted={this.state.sorting}
                    onSortedChange={onSortedChange}
                    getTrProps={selectedRowHandler}
                    showPagination={false}
                    defaultPageSize={pageSize}
                    pageSizeOptions={[pageSize]}
                    pageSize={pageSize}
                    noDataText={noDataText}
                    getNoDataProps={() => noDataProps}
                    className="-striped -highlight"
                    columns={
                        [
                            {
                                Header: "",
                                columns: [
                                    {
                                        Header: "Http Endpoints & Background Tasks",
                                        accessor: "KeyString",
                                        minWidth: 540,
                                        Cell: cellKeyPath,
                                    }
                                ]
                            },
                            {
                                Header: "App Side",
                                columns: [
                                    {
                                        Header: "Count",
                                        accessor: "Count",
                                        className: 'right-aligned',
                                        width: defaultMetricColumnWidth,
                                    },
                                    {
                                        Header: "Errors",
                                        accessor: "RequestErrors",
                                        className: 'right-aligned',
                                        width: defaultMetricColumnWidth,
                                        Cell: cellTotalErrors
                                    },
                                    {
                                        Header: "Duration",
                                        accessor: "AppDuration",
                                        className: 'right-aligned',
                                        Cell: cellNumber(x => x.AppDuration),
                                        width: defaultMetricColumnWidth,
                                    },
                                    {
                                        Header: "CPU",
                                        id: "cpuUsageTotal",
                                        accessor: x => x.AppKernelUsage + x.AppUserUsage,
                                        className: 'right-aligned',
                                        Cell: cellNumber(x => x.AppKernelUsage + x.AppUserUsage),
                                        width: defaultMetricColumnWidth,
                                    },
                                    {
                                        Header: "Kernel CPU",
                                        accessor: "AppKernelUsage",
                                        className: 'right-aligned',
                                        Cell: cellNumber(x => x.AppKernelUsage),
                                        width: defaultMetricColumnWidth,
                                    },
                                    {
                                        Header: "User CPU",
                                        accessor: "AppUserUsage",
                                        className: 'right-aligned',
                                        Cell: cellNumber(x => x.AppUserUsage),
                                        width: defaultMetricColumnWidth,
                                    },
                                ]
                            },
                            {
                                Header: "SQL Side",
                                columns: [
                                    {
                                        Header: "Commands",
                                        accessor: "SqlCounters.Requests",
                                        className: 'right-aligned',
                                        Cell: cellNumber(x => x.SqlCounters.Requests),
                                        width: defaultMetricColumnWidth,
                                    },
                                    {
                                        Header: "Errors",
                                        accessor: "SqlErrors",
                                        className: 'right-aligned',
                                        Cell: cellNumber(x => x.SqlErrors), // TODO: More precision for avg
                                        width: defaultMetricColumnWidth,
                                    },
                                    {
                                        Header: "Duration",
                                        accessor: "SqlCounters.Duration",
                                        className: 'right-aligned',
                                        Cell: cellNumber(x => x.SqlCounters.Duration),
                                        width: defaultMetricColumnWidth,
                                    },
                                    {
                                        Header: "CPU",
                                        accessor: "SqlCounters.CPU",
                                        className: 'right-aligned',
                                        Cell: cellNumber(x => x.SqlCounters.CPU),
                                        width: defaultMetricColumnWidth,
                                    },
                                    {
                                        Header: "Reads",
                                        accessor: "SqlCounters.Reads",
                                        className: 'right-aligned',
                                        Cell: cellNumber(x => x.SqlCounters.Reads),
                                        width: defaultMetricColumnWidth,
                                    },
                                    {
                                        Header: "Writes",
                                        accessor: "SqlCounters.Writes",
                                        className: 'right-aligned',
                                        Cell: cellNumber(x => x.SqlCounters.Writes),
                                        width: defaultMetricColumnWidth,
                                    },
                                ]
                            }
                        ]
                    }
                />
                
            </React.Fragment>
        );
    }
}   