import 'babel-polyfill';
import logo from './logo.svg';
import './App.css';
import NavTabs from "./Shared/NavTabs";
import dataSourceListener from "./stores/DataSourceListener";

require('typeface-roboto')
require('es6-promise').polyfill();
require('isomorphic-fetch');

function App() {

    return (
        <NavTabs />
    );
}

export default App;
