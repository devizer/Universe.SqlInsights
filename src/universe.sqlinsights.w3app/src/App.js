import 'babel-polyfill';
import './App.css';
import NavTabs from "./Shared/NavTabs";
import dataSourceListener from "./stores/DataSourceListener";
import sessionsListener from "./stores/SessionsListener";

require('typeface-roboto')
require('es6-promise').polyfill();
require('isomorphic-fetch');

function App() {

    return (
        <NavTabs />
    );
}

export default App;
