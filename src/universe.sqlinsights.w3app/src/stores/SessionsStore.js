import dispatcher from "./SessionsDispatcher";
import {EventEmitter} from "events";
import * as SessionsActions from "./SessionsActions";
import {calculateSessionFields} from "./CalculatedSessionProperties"


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
        this.sessions.push(session);
    }

    renameSession(idSession, caption) {
        const session = this.sessions.find(x => x.IdSession === idSession);
        if (session) session.Caption = caption;
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
    }

    stopSession(idSession) {
        const session = this.sessions.find(x => x.IdSession === idSession);
        if (session) {
            session.IsFinished  =true;
            session.EndedAt = new Date();
            calculateSessionFields(session);
        }
    }

    resumeSession(idSession) {
        const session = this.sessions.find(x => x.IdSession === idSession);
        if (session) {
            session.IsFinished = false;
            session.EndedAt = null;
            calculateSessionFields(session);
        }
    }

}

const sessionsStore = new SessionsStore();
dispatcher.register(sessionsStore.handleActions.bind(sessionsStore));
export default sessionsStore;

