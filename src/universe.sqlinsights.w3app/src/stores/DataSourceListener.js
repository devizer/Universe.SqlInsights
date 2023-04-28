import * as Helper from "../Helper"
import * as DataSourceActions from './DataSourceActions'
import sessionsStore from "./SessionsStore";
// import dataSourceStore from "./DataSourceStore";
import settingsStore from "./SettingsStore";

import {API_URL} from '../BuildTimeConfiguration';

// export const API_URL="http://localhost:8776/SqlInsights";
// export const API_URL="http://localhost:50420/api/v1/SqlInsights";
// export const API_URL="http://localhost:56111/AppInsight"

let prevSessionId = -1;

class DataSourceListener {

    constructor() {
        this.watchdogTick = this.watchdogTick.bind(this);
        this.requestDataSource = this.requestDataSource.bind(this);
        this.handleSessionChanged = this.handleSessionChanged.bind(this);
        this.handleSettingsChanged = this.handleSettingsChanged.bind(this);
        
        this.timerId = setInterval(this.watchdogTick, 1000);
        
        setTimeout(this.requestDataSource);
        sessionsStore.on('storeUpdated', this.handleSessionChanged);
        settingsStore.on('storeUpdated', this.handleSettingsChanged);
    }

    handleSettingsChanged() {
        if (settingsStore.getAutoUpdateSummary()) {
            setTimeout(this.requestDataSource);
        }
    }
    
    handleSessionChanged() {
        const currentSessionId = sessionsStore.getSelectedSession()?.IdSession ?? -2;
        if (currentSessionId !== prevSessionId) {
            prevSessionId = currentSessionId; 
            setTimeout(this.requestDataSource);
        }
    }


    watchdogTick() {
        if (settingsStore.getAutoUpdateSummary()) 
            this.requestDataSource();
    }

    requestDataSource() {
        const selectedSession = sessionsStore.getSelectedSession();
        const selectedSessionId = selectedSession ? selectedSession.IdSession : -1;
        const body = Helper.populateAppAndHostFiltersOfBody({IdSession: selectedSessionId});
        const req = Helper.createRequest('Summary', body);
        let apiUrl = `${API_URL}/Summary`;
        try {
            fetch(req)
                .then(response => {
                    // console.log(`Response.Status for ${apiUrl} obtained: ${response.status}`);
                    // console.log(response);
                    // console.log(response.body);
                    return response.ok ? response.json() : {error: response.status, details: response.json()}
                })
                .then(dataSource => {
                    DataSourceListener.TransformOnLoad(dataSource);
                    DataSourceActions.DataSourceUpdated(dataSource);
                    // console.log("DATA SOURCE RETRIEVED", dataSource);
                })
                .catch(error => {
                    console.error(error);
                    // DataSourceActions.ConnectionStatusUpdated(false);
                });
        } catch (err) {
            console.error(`FETCH failed for ${apiUrl}. ${err}`);
        }
    }
    
    static TransformOnLoad(dataSource) {
        dataSource.forEach(action => {
            action.KeyString = action.Key.Path.join(" \u2192 ");
        })
    }
}

const dataSourceListener = new DataSourceListener();
export default dataSourceListener;
