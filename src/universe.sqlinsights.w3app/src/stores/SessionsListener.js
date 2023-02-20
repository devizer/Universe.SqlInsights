import * as Helper from "../Helper"
import * as SessionsActions from './SessionsActions'
import moment from 'moment';

class SessionsListener {

    constructor() {
        this.watchdogTick = this.watchdogTick.bind(this);
        this.timerId = setInterval(this.watchdogTick, 1000);

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
                    SessionsActions.SessionsUpdated(sessions);
                    if (sessions != null)
                        sessions.forEach((session, index) => this.calculateSessionFields(session));
                    
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
    
    calculateSessionFields(session) {
        if (session.MaxDurationMinutes) {
            session.ExpiringDate = moment(session.StartedAt).add(session.MaxDurationMinutes, 'm').toDate();
        }
        
        session.CalculatedEnding = session.EndedAt;
        if (!session.CalculatedEnding && session.ExpiringDate) {
            session.CalculatedEnding = session.ExpiringDate;
        }
        
        // console.log('%c SESSION CALC FIELDS', 'background: #222; color: #bada55', session);

    }
}

const sessionsListener = new SessionsListener();
export default sessionsListener;

