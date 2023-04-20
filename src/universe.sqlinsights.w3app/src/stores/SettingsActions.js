import dispatcher from "./SettingsDispatcher";

export const AUTO_UPDATE_SUMMARY_UPDATED_ACTION = "AUTO_UPDATE_SUMMARY_UPDATED_ACTION";

export function AutoUpdateSummaryUpdated(autoUpdateSummary) {
    dispatcher.dispatch({
        type: AUTO_UPDATE_SUMMARY_UPDATED_ACTION,
        value: autoUpdateSummary
    })
}
