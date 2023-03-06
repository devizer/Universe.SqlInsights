import dispatcher from "./SessionsDispatcher";
import {EventEmitter} from "events";
import * as SessionsActions from "./SessionsActions";
import {calculateSessionFields} from "./CalculatedSessionProperties"
import * as Helper from "../Helper";

import sessionsListener from "./SessionsListener"



class SessionsStore extends EventEmitter {
    
    constructor() {
        super();
        // local copy per message
        this.sessions = null;
        this.selectedSession = null;
    }
    
    handleActions(action) {
        switch (action.type) {
            case SessionsActions.SESSIONS_UPDATED_ACTION: {
                this.sessions = action.value;
                this.emit("storeUpdated");
                break;
            }
            case SessionsActions.SELECTED_SESSION_UPDATED_ACTION: {
                this.selectedSession = action.value;
                this.emit("storeUpdated");
                break;
            }
            default: {
                // console.error(`Unknown action type '${action.type}' for SessionsStore`);
            }
        }
    }
    
    getSelectedSession() {
        return this.selectedSession;
    }
    
    getSessions() {
        return this.sessions;
    }

    createNewSession(caption, maxDurationMinutes) {
        const session = {Caption: caption, MaxDurationMinutes: maxDurationMinutes, StartedAt: new Date()};
        calculateSessionFields(session);
        this.sessions.push(session);
        
        this.invokeSessionManagementApi("Sessions/CreateSession", {Caption: caption, MaxDurationMinutes: maxDurationMinutes});
    }

    renameSession(idSession, caption) {
        const session = this.sessions.find(x => x.IdSession === idSession);
        if (session) session.Caption = caption;

        this.invokeSessionManagementApi("Sessions/RenameSession", {IdSession: idSession, Caption: caption});

    }

    deleteSession(idSession) {
        for( var i = 0; i < this.sessions.length; i++){
            if ( this.sessions[i].IdSession === idSession) {
                this.sessions.splice(i, 1);
            }
        }
        
        if (this.selectedSession?.IdSession === idSession) {
            this.selectedSession = null;
        }

        this.emit("storeUpdated");
        this.invokeSessionManagementApi("Sessions/DeleteSession", {IdSession: idSession});
    }

    stopSession(idSession) {
        const session = this.sessions.find(x => x.IdSession === idSession);
        if (session) {
            session.IsFinished  =true;
            session.EndedAt = new Date();
            calculateSessionFields(session);
        }

        this.invokeSessionManagementApi("Sessions/FinishSession", {IdSession: idSession});
    }

    resumeSession(idSession, maxDurationMinutes) {
        const session = this.sessions.find(x => x.IdSession === idSession);
        if (session) {
            session.IsFinished = false;
            session.EndedAt = null;
            session.MaxDurationMinutes = maxDurationMinutes ?? null;
            calculateSessionFields(session);
        }
        
        this.invokeSessionManagementApi("Sessions/ResumeSession", {IdSession: idSession, MaxDurationMinutes: maxDurationMinutes});
    }
    
    invokeSessionManagementApi(path,body) {
        const req = Helper.createRequest(path, body);
        try {
            fetch(req)
                .then(response => {
                    if (!response.ok) console.error(`SESSION ACTION REQUEST FAILED for '${req.method} ${req.url}'. status=${response.status}`, response);
                    return response.ok ? response.json() : null;
                })
                .then(ok => {
                    console.log(`SESSION ACTION REQUEST SUCCESSFULLY COMPLETED for '${req.method} ${req.url}'`);
                    // TODO TODO TODO TODO TODO TODO TODO: Trigger GetSessions
                    sessionsListener.refreshAsync();
                })
                .catch(error => {
                    console.error(`SESSION ACTION REQUEST FAILED for '${req.method} ${req.url}'.`, error);
                    // DataSourceActions.ConnectionStatusUpdated(false);
                });
        } catch (err) {
            console.error(`FETCH failed for '${req.method} ${req.url}'`, err);
        }

    }

}

const sessionsStore = new SessionsStore();
dispatcher.register(sessionsStore.handleActions.bind(sessionsStore));
export default sessionsStore;

