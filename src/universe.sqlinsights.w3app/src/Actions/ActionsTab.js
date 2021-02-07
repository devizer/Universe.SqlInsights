﻿import React, {Component} from "react";
import ActionGroupsList from "./ActionGroupsList";
import ActionList from "./ActionList";

export default class ActionsTab extends Component {
    static displayName = ActionsTab.name;
    
    state = {
        selectedAction: null,
    }
    
    constructor(props) {
        super(props);
        
    }
    
    render() {
        
        const onActionSelected = action => {
            this.setState({selectedAction: action});
        }
        
        return (
            <React.Fragment>
                <ActionGroupsList onActionSelected={onActionSelected}/>
                <ActionList keyPath={this.state.selectedAction ? this.state.selectedAction.Key.Path : null} />
            </React.Fragment>
        )
    }
}
