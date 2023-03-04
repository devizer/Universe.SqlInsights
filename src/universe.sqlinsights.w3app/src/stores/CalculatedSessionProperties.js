import moment from "moment/moment";

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
