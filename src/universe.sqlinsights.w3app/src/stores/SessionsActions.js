import dispatcher from "./DataSourceDispatcher";

export const SESSION_UPDATED_ACTION = "SESSION_UPDATED_ACTION";

export function DataSourceUpdated(sessions) {
    dispatcher.dispatch({
        type: SESSION_UPDATED_ACTION,
        value: sessions
    })
}

