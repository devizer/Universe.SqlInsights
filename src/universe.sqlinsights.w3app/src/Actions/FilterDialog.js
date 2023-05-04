import * as SettingsActions from "../stores/SettingsActions"
import sessionsStore from "../stores/SessionsStore";
import settingsStore from "../stores/SettingsStore";
import React, {Component} from "react";
import PropTypes from "prop-types";

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

// AutoComplete
import Checkbox from '@material-ui/core/Checkbox';
import Autocomplete from '@material-ui/lab/Autocomplete';
import CheckBoxOutlineBlankIcon from '@material-ui/icons/CheckBoxOutlineBlank';
import CheckBoxIcon from '@material-ui/icons/CheckBox';
import * as Helper from "../Helper";
import * as SessionsActions from "../stores/SessionsActions";
import ReactComponentWithPerformance from "../Shared/ReactComponentWithPerformance";

const checkBoxBlankIcon = <CheckBoxOutlineBlankIcon fontSize="small" />;
const checkBoxCheckedIcon = <CheckBoxIcon fontSize="small" />;

function CheckboxesTags({id, label, placeholder, allValues, value, onChange}) {
    const handleChange = (event, val, reason) => {
        console.log(`FILTER ${label} (${reason}). Checked: ${event.target.checked}.`, val )
        if (onChange) onChange(val);
    };
    
    return (
        <Autocomplete
            multiple
            id={id ?? "checkboxes-filter"}
            options={allValues}
            value={value ?? []}
            onChange={handleChange}
            disableCloseOnSelect
            getOptionLabel={(option) => option.title}
            renderOption={(option, { selected }) => (
                <React.Fragment>
                    <Checkbox
                        icon={checkBoxBlankIcon}
                        checkedIcon={checkBoxCheckedIcon}
                        style={{ marginRight: 8 }}
                        checked={selected}
                    />
                    {option.title}
                </React.Fragment>
            )}
            style={{ width: "100%" }}
            renderInput={(params) => (
                <TextField {...params} variant="outlined" label={label} placeholder={placeholder} />
            )}
        />
    );
}

export default class FilterDialog extends Component {
    static displayName = FilterDialog.name;
    internalName = () => "FilterDialog";

    static propTypes = {
        dialogVisible: PropTypes.bool,
        onClose: PropTypes.func,
    }

    constructor(props) {
        super(props);

        this.populateFilterDictionaries = this.populateFilterDictionaries.bind(this);
        
        settingsStore.getAppFilter(); // Array Of Strings
        settingsStore.getHostFilter(); // Array of Strings
        
        this.state = {
            dialogVisible: props.dialogVisible,
            filtersDictionary: null, // updated by fetch (apps and hosts as is from backend)
            // rename to allTheApps and allTheHosts 
            appsFilters: null,       // All the Apps fetched by backend (objects with calculated title property)
            hostsFilters: null,      // All the Hosts fetched by backend (objects with calculated title property)
            selectedApps: null,  // state of the UI, e.g. array of Objects
            selectedHosts: null, // state of the UI, e.g. array of Objects
        };
    }
    
    componentWillReceiveProps(nextProps, nextContext) {
        if (nextProps.dialogVisible !== this.props.dialogVisible) {
            this.setState({dialogVisible: nextProps.dialogVisible});
            if (nextProps.dialogVisible) this.populateFilterDictionaries();
        }
    }
    
    populateFilterDictionaries() {
        const req = Helper.createRequest('PopulateFilters', {});
        try {
            fetch(req)
                .then(response => {
                    if (!response.ok) console.error(`FETCH failed for '${req.method} ${req.url}'. status=${response.status}`, response);
                    return response.ok ? response.json() : null;
                })
                .then(filters => {
                    if (filters != null) {
                        console.log(`POPULATING FILTERS. Apps: ${filters.ApplicationList?.length}. Hosts: ${filters.HostIdList?.length}`, filters);
                        const appsFilters = filters.ApplicationList.map(x => ({ ...x, title: x.App }));
                        const hostsFilters = filters.HostIdList.map(x => ({ ...x, title: x.HostId }));
                        const stringsOfAppsFilter = settingsStore.getAppFilter() ?? [];
                        const stringsOfHostsFilter = settingsStore.getHostFilter() ?? [];
                        const valueForAppsCombo = appsFilters.filter(x => stringsOfAppsFilter.find(y => y === x.App));
                        const valueForHostsCombo = hostsFilters.filter(x => stringsOfHostsFilter.find(y => y === x.HostId));
                        // timeout for layout debug only 
                        setTimeout(() => this.setState({
                            filtersDictionary: filters,
                            appsFilters,
                            hostsFilters,
                            selectedApps: valueForAppsCombo,
                            selectedHosts: valueForHostsCombo,
                        }), 0);
                    }
                })
                .catch(error => {
                    console.error(error);
                });
        } catch (err) {
            console.error(`FETCH failed for '${req.method} ${req.url}'. ${err}`);
        }
        
    }

    render() {

        const handleClose = () => {
            this.setState({dialogVisible: false});
            if (this.props.onClose) this.props.onClose();
        }

        const handleClear = (event) => {};
        const handleApply = (event) => {};
        
        // Kind: "Hosts" | "Apps"
        // Value: Array of Objects
        const handleValueChanged = kind => value => {
            const newState = {};
            newState[`selected${kind}`] = value;
            this.setState(newState);
            // DONE: map objects to strings and call either 
            // SettingsActions.AppFilterUpdated(arrayOfStrings)
            // or 
            // SettingsActions.HostFilterUpdated(arrayOfStrings)
            if (kind === "Apps") {
                SettingsActions.AppFilterUpdated(value.map(x => x.App));
            }
            else if (kind === "Hosts") {
                SettingsActions.HostFilterUpdated(value.map(x => x.HostId));
            }
            else     
                throw new Error(`Unknown kind parameter '${kind}'`);
        } 
        
        return (
            
            <Dialog open={this.state.dialogVisible} onClose={handleClose} aria-labelledby="form-dialog-title" fullWidth={true} maxWidth="md">
                <DialogTitle id="form-dialog-title">Filter endpoints and background tasks by apps and/or hosts</DialogTitle>    
                <DialogContent>
                    {this.state.filtersDictionary !== null && <React.Fragment>
                        <CheckboxesTags id="filter-app" label="Applications" placeholder="app" allValues={this.state.appsFilters ?? []} value={this.state.selectedApps} onChange={handleValueChanged("Apps")} />
                        <div style={{height: 12}}>&nbsp;</div>
                        <CheckboxesTags id="filter-host" label="Hosts" placeholder="host" allValues={this.state.hostsFilters ?? []} value={this.state.selectedHosts} onChange={handleValueChanged("Hosts")} />
                    </React.Fragment>}
                    {this.state.filtersDictionary === null &&
                        <div style={{width:"100%", paddingTop: 52, paddingBottom: 52, color: "#888"}} className={"center-aligned"}>waiting for lists ...</div>
                    }
                </DialogContent>
                <DialogActions>
                    <Typography variant="caption" display="block" gutterBottom noWrap className={"right-aligned"} style={{padding: 16}}>
                        <Button onClick={handleClose} variant="contained" color="primary">Close</Button>
                    </Typography>
                </DialogActions>
            </Dialog>
        );
    }
}

