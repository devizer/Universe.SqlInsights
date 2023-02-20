import dispatcher from "./DataSourceDispatcher";
import {EventEmitter} from "events";
import * as SessionsActions from "./SessionsActions";

class SessionsStore extends EventEmitter {
    
    constructor() {
        super();
        // local copy per message
        this.sessions = null;
    }
    
    handleActions(action) {
        switch (action.type) {
            // a cast per message
            case SessionsActions.SESSION_UPDATED_ACTION: {
                this.sessions = action.value;
                this.emit("storeUpdated");
                break;
            }
            default: {
                // console.error(`Unknown action type '${action.type}' for SessionsStore`);
            }
        }
    }
    
    getSessions() {
        return this.sessions;
    }
}

const sessionsStore = new SessionsStore();
dispatcher.register(sessionsStore.handleActions.bind(sessionsStore));
export default sessionsStore;

