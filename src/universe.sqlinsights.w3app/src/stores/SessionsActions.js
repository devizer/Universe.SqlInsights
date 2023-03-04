import dispatcher from "./SessionsDispatcher";

export const SESSIONS_UPDATED_ACTION = "SESSION_UPDATED_ACTION";
export const SELECTED_SESSION_UPDATED_ACTION = "SELECTED_SESSION_UPDATED_ACTION";

/*
export const ListActions = {
    New: "New",
    Rename: "Rename",
    Delete: "Delete",
    Resume: "Resume",
    Stop: "Stop",
}
    

export function NewSession(caption, maxDurationMinutes) {
    dispatcher.dispatch({
        type: ListActions.New,
        value: {caption, maxDurationMinutes},
    });
}

export function RenameSession(idSession, caption) {
    dispatcher.dispatch({
        type: ListActions.Rename,
        value: {idSession, caption},
    });
}

export function DeleteSession(idSession) {
    dispatcher.dispatch({
        type: ListActions.Delete,
        value: {idSession},
    });
}

export function StopSession(idSession) {
    dispatcher.dispatch({
        type: ListActions.Stop,
        value: {idSession},
    });
}

export function ResumeSession(idSession) {
    dispatcher.dispatch({
        type: ListActions.Stop,
        value: {idSession},
    });
}
*/


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

