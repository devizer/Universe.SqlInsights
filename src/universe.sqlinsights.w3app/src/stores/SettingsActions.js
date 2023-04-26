import dispatcher from "./SettingsDispatcher";

export const AUTO_UPDATE_SUMMARY_UPDATED_ACTION = "AUTO_UPDATE_SUMMARY_UPDATED_ACTION";
export const FILTER_APP_UPDATED_ACTION = "FILTER_APP_UPDATED_ACTION";
export const FILTER_HOST_UPDATED_ACTION = "FILTER_HOST_UPDATED_ACTION";

export function AutoUpdateSummaryUpdated(autoUpdateSummary) {
    dispatcher.dispatch({
        type: AUTO_UPDATE_SUMMARY_UPDATED_ACTION,
        value: autoUpdateSummary
    })
}

export function AppFilterUpdated(appFilter) {
    dispatcher.dispatch({
        type: FILTER_APP_UPDATED_ACTION,
        value: appFilter
    })
}

export function HostFilterUpdated(hostFilter) {
    dispatcher.dispatch({
        type: FILTER_HOST_UPDATED_ACTION,
        value: hostFilter
    })
}
