// import 'babel-polyfill';
import './App.css';
import NavTabs from "./Shared/NavTabs";
import dataSourceListener from "./stores/DataSourceListener";
import sessionsListener from "./stores/SessionsListener";
import {useEffect, useState} from "react";
import ThemeStore from "./stores/ThemeStore";

require('typeface-roboto')
require('es6-promise').polyfill();
require('isomorphic-fetch');

function App() {

    const [systemTheme, setSystemTheme] = useState(ThemeStore.getSystemTheme());
    const onThemeChanged = newTheme => {
        console.log(`%c SYSTEM THEME CHANGED: ${newTheme}`, "color: DarkGreen");
        setSystemTheme(newTheme);
    }
    
    useEffect(() => {
        ThemeStore.on(onThemeChanged);
        return () => ThemeStore.off(onThemeChanged);
    });
    
    return (
        <NavTabs theme={systemTheme} />
    );
}

export default App;
