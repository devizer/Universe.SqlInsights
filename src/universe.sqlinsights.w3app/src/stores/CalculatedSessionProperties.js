﻿import moment from "moment/moment";

export function calculateSessionFields(session) {
    if (session.MaxDurationMinutes) {
        session.ExpiringDate = moment(session.StartedAt).add(session.MaxDurationMinutes, 'm').toDate();
    }

    session.CalculatedEnding = session.EndedAt;
    if (!session.CalculatedEnding && session.ExpiringDate) {
        session.CalculatedEnding = session.ExpiringDate;
    }

    // Doesn't work
    // session.isExpired = () => (Boolean(session.ExpiringDate)) && (session.ExpiringDate < new Date());
    // session.isExpired = session.isExpired.bind(session);

}

export function isSessionAlive(session) {
    const isStopped = Boolean(session?.IsFinished);
    // const isExpired = this.state.sessionOfMenu && this.state.sessionOfMenu.ExpiringDate < new Date();
    const isExpired = session?.ExpiringDate && session.ExpiringDate < new Date();
    return !isStopped && !isExpired;
}
