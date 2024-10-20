const topics = new Map();

class TopicMessenger {
    privateData = {
        listeners: [],
        value: undefined,
        notifyAll: ($this, value, previousValue) => {
            $this.listeners.forEach(listener => {
                if (listener) listener(value, previousValue);
            });
        }    
    }
    
    constructor(defaultValue) {
        this.privateData.value = typeof defaultValue === "function" ? defaultValue() : defaultValue;
    }
    
    getValue = () => this.privateData.value;
    
    raiseUpdate = newValue => {
        this.privateData.value = newValue;
        this.privateData.notifyAll(this, newValue, this.privateData.value);
    }

    subscribe = listener => this.privateData.listeners.push(listener); 

    unsubscribe = listener => {
        const index = this.privateData.listeners.indexOf(listener);
        if (index !== -1) {
            this.privateData.listeners.splice(index, 1);
        }
    }
}


export const makeTopicDispatcher = (topicName, defaultValue) => {
    let ret = topics.get(topicName);
    if (!ret) {
        ret = new TopicMessenger(defaultValue);
        topics.set(topicName, ret);
    }
    
    return ret;
}
