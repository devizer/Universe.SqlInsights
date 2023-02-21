import dispatcher from "./SessionsDispatcher";
import {EventEmitter} from "events";
import * as SessionsActions from "./SessionsActions";

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
}

const sessionsStore = new SessionsStore();
dispatcher.register(sessionsStore.handleActions.bind(sessionsStore));
export default sessionsStore;

