import dispatcher from "./SettingsDispatcher";
import {EventEmitter} from "events";
import * as SettingsActions from "./SettingsActions";

class SettingsStore extends EventEmitter {

    constructor() {
        super();
        this.autoUpdateSummary = true;
    }

    handleActions(action) {
        switch (action.type) {
            case SettingsActions.AUTO_UPDATE_SUMMARY_UPDATED_ACTION: {
                this.autoUpdateSummary = action.value;
                this.emit("storeUpdated");
                break;
            }
            default: {
            }
        }
    }

    getAutoUpdateSummary() {
        return this.autoUpdateSummary;
    }
    
}

const settingsStore = new SettingsStore();
dispatcher.register(settingsStore.handleActions.bind(settingsStore));
export default settingsStore;
