import * as Helper from "../Helper"
import React, { Component } from 'react';
import PropTypes from 'prop-types';
import Radio from '@material-ui/core/Radio';
import RadioGroup from '@material-ui/core/RadioGroup';
import FormControlLabel from '@material-ui/core/FormControlLabel';
import ReactTable from "react-table";
import "react-table/react-table.css";
import Switch from '@material-ui/core/Switch';

import ListAltIcon from '@material-ui/icons/ListAlt';
import IconButton from '@material-ui/core/IconButton';

import dataSourceStore from "../stores/DataSourceStore";
import * as DataSourceActions from "../stores/DataSourceActions"
import * as SettingsActions from "../stores/SettingsActions"
import {ActionKeyPathUi} from "../Shared/ActionKeyPathUi";
import ActionList from "./ActionList";
import * as DocumentVisibilityStore from "../stores/DocumentVisibilityStore";
import sessionsStore from "../stores/SessionsStore";
import settingsStore from "../stores/SettingsStore";
import {Icon} from "@material-ui/core";
import {LiveUpdateSwitch} from "./LiveUpdateSwitch";
import FilterDialog from "./FilterDialog";
import ReactComponentWithPerformance from "../Shared/ReactComponentWithPerformance";

const noDataProps = {style:{color:"gray", marginTop:30, border: "1px solid grey"}};
let renderCount = 0;

export default class ActionGroupsList extends ReactComponentWithPerformance {
    static displayName = ActionGroupsList.name;
    internalName = () => "ActionGroupList";


    static propTypes = {
        onActionSelected: PropTypes.func
    }

    constructor(props) {
        super(props);

        this.updateDataSource = this.updateDataSource.bind(this);
        this.updateSettings = this.updateSettings.bind(this);
        this.handleVisibility = this.handleVisibility.bind(this);

        this.state = {
            actions: null,
            selected: null, // TODO: REMOVE IT
            selectedRow: null,
            kind: 'total', // or 'average'
            sorting: [{id: "AppDuration", desc: true}],
            autoUpdateSummary: settingsStore.getAutoUpdateSummary(),
            filterDialogVisible: false,
        };
    }
    
    handleVisibility(isVisible) {
        Helper.toConsole(`handleVisibility(${isVisible})`);
    }

    componentDidMount()
    {
        let x = dataSourceStore.on('storeUpdated', this.updateDataSource);
        settingsStore.on('storeUpdated', this.updateSettings);
        DocumentVisibilityStore.on(this.handleVisibility);
    }

    componentWillUnmount() {
        dataSourceStore.off('storeUpdated', this.updateDataSource);
        settingsStore.off('storeUpdated', this.updateSettings);
    }

    updateDataSource() {
        this.setState({actions: dataSourceStore.getDataSource()});
    }
    
    updateSettings() {
        // Need to update filter label if auto update is off
        this.setState({triggerSettingsUpdated: new Date()})
    }
    
    render() {
        Helper.toConsole(`${++renderCount} Rendering «ActionGroupsList», name=${this.constructor.name}, displayName2=${this.constructor.displayName2}`);
        const isLoaded = this.state.actions !== null;
        const actions = this.state.actions === null ? [] : this.state.actions;
        Helper.toConsole(`Rendering TOTAL ACTION GROUPS`, actions.length);
        const handleChangeSummaryKind = (event) => {
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
        const cellCount = row => formatNumber(row.original.Count);
        
        

        const selectedRowHandler = (state, rowInfo, column) => {
            if (rowInfo && rowInfo.row) {
                const legacyIsSelected = rowInfo.index === this.state.selected; // remove it 
                const selectedRow = this.state.selectedRow;
                const selectedKeyString = selectedRow?.KeyString;
                const currentKeyString = rowInfo.original?.KeyString;
                const isSelected = currentKeyString && currentKeyString === selectedKeyString; 
                return {
                    onClick: (e) => {
                        const selectedRow = rowInfo.original;
                        // console.warn("REPLACE SELECTED BY INDEX BY Selected by Key Path. Clicked Row is", selectedRow);
                        this.setState({
                            selected: rowInfo.index,
                            selectedRow: selectedRow,
                        });
                        Helper.toConsole("Action KeyPath Selected", selectedRow);
                        if (this.props.onActionSelected)
                            this.props.onActionSelected(selectedRow);
                    },
                    style: {
/*
                        background: isSelected ? '#4f9a94' : 'white',
                        color: isSelected ? 'white' : 'black',
                        cursor: "pointer",
*/
                    },
                    className: isSelected ? "SelectedActionKeyRow SelectedTableRow" : "UnSelectedActionKeyRow UnSelectedTableRow"
                }
            } else {
                return {}
            }
        }
        
        const defaultMetricColumnWidth = 86;
        let noDataText = isLoaded ? "no actions triggered" : "waiting for cells";
        if (!sessionsStore.getSelectedSession()) noDataText = "select a session";
        
        const autoUpdateSummary = this.state.autoUpdateSummary;
        const handleAutoUpdateSummary = (event) => {
            const newAutoUpdateSummary = event.target.checked;
            this.setState({autoUpdateSummary: newAutoUpdateSummary });
            SettingsActions.AutoUpdateSummaryUpdated(newAutoUpdateSummary);
        };

        const handleCloseFilterDialog = (event) => {
            this.setState({filterDialogVisible: false});
        }
        
        const handleOpenFilterDialog = (event) => {
            this.setState({filterDialogVisible: true});
        }
        
        const getFilterLabel = () => {
            const appFilters = settingsStore.getAppFilter() ?? [];
            const hostFilters = settingsStore.getHostFilter() ?? [];
            const appFilterText = appFilters.length === 0 ? "any app" : appFilters.length === 1 ? "1 app" : `${appFilters.length} apps`;
            const hostFilterText = hostFilters.length === 0 ? "any host" : hostFilters.length === 1 ? "1 host" : `${hostFilters.length} hosts`;
            return `${appFilterText}, ${hostFilterText}`;
        }
            
        return (
            <React.Fragment>
                <RadioGroup row aria-label="kind" name="kind" value={this.state.kind} onChange={handleChangeSummaryKind} className='center-aligned padding-top'>
                    <div style={{textAlign: 'center', width: '100%'}}>
                        <FormControlLabel control={<null />} label="Live update:" style={{marginRight:6}} />
                        <LiveUpdateSwitch autoUpdateSummary={autoUpdateSummary} handleAutoUpdateSummary={handleAutoUpdateSummary} />
                        
                        <FormControlLabel control={<null />} label="" style={{paddingLeft: 28, paddingRight: 28}} />
                        
                        <FormControlLabel control={<null />} label="Display:" style={{marginRight2:-4}} />
                        <FormControlLabel value="average" control={<Radio />} label="Average" />
                        <FormControlLabel value="total" control={<Radio />} label="Total" />
                        
                        <FormControlLabel control={<null />} label="" style={{paddingLeft: 28, paddingRight: 28}} />

                        <FormControlLabel control={<null />} label={`Filter: ${getFilterLabel()}`} style={{marginRight:-4}} />
                        <IconButton color="default" aria-label="filter by app or host" component="span" onClick={handleOpenFilterDialog}>
                            <ListAltIcon />
                        </IconButton>                        
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
                    className="ActionKeysRT -striped -highlight"
                    columns={
                        [
                            {
                                Header: "",
                                columns: [
                                    {
                                        Header: "Service Endpoints, Background Tasks, and Queue Messages",
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
                                        Cell: cellCount
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
                                    {
                                        Header: "Rows",
                                        accessor: "SqlCounters.RowCounts",
                                        className: 'right-aligned',
                                        Cell: cellNumber(x => x.SqlCounters.RowCounts),
                                        width: defaultMetricColumnWidth,
                                    },
                                ]
                            }
                        ]
                    }
                />
                
                <FilterDialog dialogVisible={this.state.filterDialogVisible} onClose={handleCloseFilterDialog} />
                
            </React.Fragment>
        );
    }
}   