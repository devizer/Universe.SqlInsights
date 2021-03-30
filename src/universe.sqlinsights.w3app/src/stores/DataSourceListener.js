import * as Helper from "../Helper"
import * as DataSourceActions from './DataSourceActions'
import DataSourceStore from "./DataSourceStore";

// export const API_URL="http://localhost:8776/SqlInsights";
export const API_URL="http://localhost:50420/api/v1/SqlInsights";
// export const API_URL="http://localhost:56111/AppInsight"

class DataSourceListener {

    constructor() {
        this.watchdogTick = this.watchdogTick.bind(this);
        this.timerId = setInterval(this.watchdogTick, 1000);
        
        setTimeout(this.watchdogTick);
    }

    watchdogTick() {
        const req = Helper.createRequest('Summary', {IdSession: 0, AppName: null, HostId: null});
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
