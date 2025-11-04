global.ApplicationLevelTriggers = {};
export const notifyTrigger = (triggerName) => {
    if (! window.LoadingStartedAt) { 
      window.LoadingStartedAt = (window.performance && window.performance.now) ? window.performance.now() : +new Date();
      window.ApplicationLevelTriggers = {};
    }
    const start = window.LoadingStartedAt || 0;
    const current = (window.performance && window.performance.now) ? window.performance.now() : +new Date();
    if (window.ApplicationLevelTriggers === undefined) window.ApplicationLevelTriggers = {};
    let prev = window.ApplicationLevelTriggers[triggerName];
    if (prev === undefined) {
        prev = {first: current - start, counter: 0};
        window.ApplicationLevelTriggers[triggerName] = prev;
    }
    
    prev.last = current - start;
    prev.counter = prev.counter + 1;
};

export const getAppLevelTriggers = () => {
    if (! window.LoadingStartedAt) {
        window.LoadingStartedAt = (window.performance && window.performance.now) ? window.performance.now() : +new Date();
        window.ApplicationLevelTriggers = {};
    }
    
    return window.ApplicationLevelTriggers;
}