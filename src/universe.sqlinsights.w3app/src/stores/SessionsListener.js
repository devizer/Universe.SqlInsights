import * as Helper from "../Helper"
import * as SessionsActions from './SessionsActions'

class SessionsListener {

    constructor() {
        this.watchdogTick = this.watchdogTick.bind(this);
        this.timerId = setInterval(this.watchdogTick, 1000);

        setTimeout(this.watchdogTick);
    }

    watchdogTick() {
        const req = Helper.createRequest('Sessions/Index', {});
        try {
            fetch(req)
                .then(response => {
                    return response.ok ? response.json() : {error: response.status, details: response.json()}
                })
                .then(sessions => {
                    SessionsActions.DataSourceUpdated(sessions);
                    console.log("SESSIONS RETRIEVED", sessions);
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

