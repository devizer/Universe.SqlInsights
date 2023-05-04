import {Component} from "react";

export default class ReactComponentWithPerformance extends Component {
    
    
    constructor(props) {
        super(props);
    }
    
    internalName = () => this.constructor.displayName ?? this.constructor.name;
    
    renderCount = 0;
    
    // TODO: DOES NOT Work for FilterDialog
    shouldComponentUpdate(nextProps, nextState, nextContext) {
        this.startRenderAt = window?.performance?.now ? window.performance.now() : +new Date();
        return true;
    }
    
    getSnapshotBeforeUpdate(prevProps, prevState) {
        if (this.startRenderAt && this.renderCount <= 100) {
            const name = this.internalName(); 
            const endRenderAt = window?.performance?.now ? window.performance.now() : +new Date();
            const renderTime = (endRenderAt - this.startRenderAt);
            const count = ++this.renderCount;
            console.log(`Render #${count} «${name}» %c${renderTime.toLocaleString("en-US", {useGrouping: true, maximumFractionDigits: 1})}`, "font-weight: bold");
        }
    }
    
}


