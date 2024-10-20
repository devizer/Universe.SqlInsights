import { makeTopicDispatcher } from '../Shared/TopicDispatcher'

// CheckBox: .raiseUpdate(true|false)
// Tables: .subscribe(isCondensed => {}) + unsubscribe;
const condensedFontStore = makeTopicDispatcher("Condensed Font", () => false);
export default condensedFontStore;
