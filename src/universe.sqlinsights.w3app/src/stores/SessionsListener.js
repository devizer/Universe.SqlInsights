import * as Helper from "../Helper"
import * as SessionsActions from './SessionsActions'
import {calculateSessionFields} from "./CalculatedSessionProperties"

class SessionsListener {

    constructor() {
        this.watchdogTick = this.watchdogTick.bind(this);
        this.refreshAsync = this.refreshAsync.bind(this);
        this.timerId = setInterval(this.watchdogTick, 15*1000);

        this.refreshAsync();
    }
    
    refreshAsync() {
        setTimeout(this.watchdogTick);
    }

    watchdogTick() {
        const req = Helper.createRequest('Sessions/Sessions', {});
        try {
            fetch(req)
                .then(response => {
                    if (!response.ok) console.error(`FETCH failed for '${req.method} ${req.url}'. status=${response.status}`, response);
                    return response.ok ? response.json() : null;
                })
                .then(sessions => {
                    if (sessions != null) {
                        sessions?.map((session, index) => calculateSessionFields(session));
                    }
                    else {
                        console.error("!!!! SKIPPED calculateSessionFields");
                    }
                    SessionsActions.SessionsUpdated(sessions);
                    // console.log("SESSIONS RETRIEVED", sessions);
                })
                .catch(error => {
                    console.error(error);
                    // DataSourceActions.ConnectionStatusUpdated(false);
                });
        } catch (err) {
            console.error(`FETCH failed for '${req.method} ${req.url}'. ${err}`);
        }
    }
    
}

const sessionsListener = new SessionsListener();
export default sessionsListener;

