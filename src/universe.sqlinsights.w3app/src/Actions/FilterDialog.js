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
import {calculateSessionFields} from "../stores/CalculatedSessionProperties";
import * as SessionsActions from "../stores/SessionsActions";

const checkBoxBlankIcon = <CheckBoxOutlineBlankIcon fontSize="small" />;
const checkBoxCheckedIcon = <CheckBoxIcon fontSize="small" />;

// Top 100 films as rated by IMDb users. http://www.imdb.com/chart/top
const top100Films = [
    { title: 'The Shawshank Redemption', year: 1994 },
    { title: 'The Godfather', year: 1972 },
    { title: 'The Godfather: Part II', year: 1974 },
    { title: 'The Dark Knight', year: 2008 },
    { title: '12 Angry Men', year: 1957 },
    { title: "Schindler's List", year: 1993 },
    { title: 'Pulp Fiction', year: 1994 },
    { title: 'The Lord of the Rings: The Return of the King', year: 2003 },
    { title: 'The Good, the Bad and the Ugly', year: 1966 },
    { title: 'Fight Club', year: 1999 },
    { title: 'The Lord of the Rings: The Fellowship of the Ring', year: 2001 },
    { title: 'Star Wars: Episode V - The Empire Strikes Back', year: 1980 },
    { title: 'Forrest Gump', year: 1994 },
    { title: 'Inception', year: 2010 },
    { title: 'The Lord of the Rings: The Two Towers', year: 2002 },
    { title: "One Flew Over the Cuckoo's Nest", year: 1975 },
    { title: 'Goodfellas', year: 1990 },
    { title: 'The Matrix', year: 1999 },
    { title: 'Seven Samurai', year: 1954 },
    { title: 'Star Wars: Episode IV - A New Hope', year: 1977 },
    { title: 'City of God', year: 2002 },
    { title: 'Se7en', year: 1995 },
    { title: 'The Silence of the Lambs', year: 1991 },
    { title: "It's a Wonderful Life", year: 1946 },
    { title: 'Life Is Beautiful', year: 1997 },
    { title: 'The Usual Suspects', year: 1995 },
    { title: 'Léon: The Professional', year: 1994 },
    { title: 'Spirited Away', year: 2001 },
    { title: 'Saving Private Ryan', year: 1998 },
    { title: 'Once Upon a Time in the West', year: 1968 },
    { title: 'American History X', year: 1998 },
    { title: 'Interstellar', year: 2014 },
];

function CheckboxesTags({id, label, placeholder}) {
    return (
        <Autocomplete
            multiple
            id={id ?? "checkboxes-filter"}
            options={top100Films}
            defaultValue={[top100Films[2],top100Films[3]]}
            onChange={(e) => console.log(`FILTER ${label}`, e.target.checked)}
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

    static propTypes = {
        dialogVisible: PropTypes.bool,
        onClose: PropTypes.func,
    }

    constructor(props) {
        super(props);

        this.populateFilterDictionaries = this.populateFilterDictionaries.bind(this);
        
        this.state = {
            dialogVisible: props.dialogVisible,
            filtersDictionary: null, // updated by fetch
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
                        // timeout for layout debug only 
                        setTimeout(() => this.setState({filtersDictionary: filters}), 0);
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
        
        return (
            
            <Dialog open={this.state.dialogVisible} onClose={handleClose} aria-labelledby="form-dialog-title" fullWidth={true} maxWidth="md">
                <DialogTitle id="form-dialog-title">Filter endpoints and background tasks by apps and/or hosts</DialogTitle>    
                <DialogContent>
                    {this.state.filtersDictionary !== null && <React.Fragment>
                        <CheckboxesTags id="filter-app" label="Applications" placeholder="app"/>
                        <div style={{height: 12}}>&nbsp;</div>
                        <CheckboxesTags id="filter-host" label="Hosts" placeholder="host" />
                    </React.Fragment>}
                    {this.state.filtersDictionary === null && <React.Fragment>
                        <div style={{width:"100%", paddingTop: 52, paddingBottom: 52, color: "#888"}} className={"center-aligned"}>waiting for lists</div>
                    </React.Fragment>}
                </DialogContent>
                <DialogActions>
                    <Typography variant="caption" display="block" gutterBottom noWrap className={"right-aligned"} style={{padding: 16}}>

                        <Button onClick={handleClose} variant="outlined" color="primary">
                            Cancel
                        </Button>
                        &nbsp;&nbsp;&nbsp;

                        <Button onClick={handleClear} variant="contained" color="primary">
                            Clear
                        </Button>
                        &nbsp;&nbsp;&nbsp; 

                        <Button onClick={handleApply} variant="contained" color="primary">
                            Apply
                        </Button>
                        
                        
                    </Typography>

                </DialogActions>
            </Dialog>
        );
    }
}

