import dispatcher from "./SessionsDispatcher";

export const SESSIONS_UPDATED_ACTION = "SESSION_UPDATED_ACTION";
export const SELECTED_SESSION_UPDATED_ACTION = "SELECTED_SESSION_UPDATED_ACTION";

export function SessionsUpdated(sessions) {
    dispatcher.dispatch({
        type: SESSIONS_UPDATED_ACTION,
        value: sessions
    })
}

export function SelectedSessionUpdated(selectedSession) {
    dispatcher.dispatch({
        type: SELECTED_SESSION_UPDATED_ACTION,
        value: selectedSession
    })
}

