import dispatcher from "./SettingsDispatcher";
import {EventEmitter} from "events";
import * as SettingsActions from "./SettingsActions";

class SettingsStore extends EventEmitter {

    constructor() {
        super();
        this.autoUpdateSummary = true;
        this.appFilter = null;
        this.hostFilter = null;
    }

    handleActions(action) {
        switch (action.type) {
            case SettingsActions.AUTO_UPDATE_SUMMARY_UPDATED_ACTION: {
                this.autoUpdateSummary = action.value;
                this.emit("storeUpdated");
                break;
            }
            case SettingsActions.FILTER_APP_UPDATED_ACTION: {
                this.appFilter = action.value;
                console.log(`SettingsStore::appFilter updated: «${this.appFilter}»`);
                this.emit("storeUpdated");
                break;
            }
            case SettingsActions.FILTER_HOST_UPDATED_ACTION: {
                this.hostFilter = action.value;
                console.log(`SettingsStore::hostFilter updated: «${this.hostFilter}»`);
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
    
    getAppFilter() {
        return this.appFilter;
    }
    
    getHostFilter() {
        return this.hostFilter;
    }
    
}

const settingsStore = new SettingsStore();
dispatcher.register(settingsStore.handleActions.bind(settingsStore));
export default settingsStore;
