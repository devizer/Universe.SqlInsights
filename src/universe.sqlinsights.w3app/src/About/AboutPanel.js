import './About.css';
import * as Helper from "../Helper"
import React, { Component } from 'react';
import {API_URL} from '../BuildTimeConfiguration';
import AppVersion from "../AppVersion.json"
import url from 'url';
import PropTypes from "prop-types";
import ReactComponentWithPerformance from "../Shared/ReactComponentWithPerformance";
import {ActionKeyPathUi} from "../Shared/ActionKeyPathUi";
import {Container, Paper} from "@material-ui/core";

export default class AboutPanel extends ReactComponentWithPerformance {
    static displayName = AboutPanel.name;
    internalName = () => "AboutPanel";

    constructor(props) {
        super(props);

        this.fetchAboutResponse = this.fetchAboutResponse.bind(this);
        
        this.state = {
            about: {}
        };
    }

    componentDidMount()
    {
        this.fetchAboutResponse();
    }
    
    fetchAboutResponse() {
        const req = Helper.createRequest('About/Index', {});
        try {
            fetch(req)
                .then(response => {
                    return response.ok ? response.json() : {error: response.status, details: response.json()}
                })
                .then(aboutResponse => {
                    this.setState({about: aboutResponse})
                })
                .catch(err => {
                    console.error(`FETCH failed for ${req.url}. ${err}`);
                });
        } catch (err) {
            console.error(`FETCH failed for ${req.url}. ${err}`);
        }
    }

    render() {
/*
        const apiUrl = url.parse(API_URL);
        const apiSchema = apiUrl.protocol;
        const apiHost = apiUrl.host;
        const apiPort = apiUrl.port;
*/
        const about = this.state.about ?? {};
        const rows = [
            {Id: 'API Url', Value: API_URL},
            {Id: 'API Version', Value: about.AppVersion},
            {Id: 'UI Version', Value: AppVersion.Version},
            {Id: 'Storage Server', Value: about.DbServer},
            {Id: 'Storage Database', Value: about.DbServer},
        ];
        const cells = [
            ['API Url', API_URL],
            ['API Version', about.AppVersion],
            ['UI Version', AppVersion.Version],
            ['Storage Server', about.DbServer],
            ['Storage Database',about.DbCatalog],        
        ];
        return <> 
        <Container maxWidth="md" style={{boxShadow: "-1px -1px 7px 0px #989997, 1px 1px 2px 0px #CECECE", padding: "10px 20px", marginTop:24}}>
            <h3 className="center-aligned">
                SQL Server Sixth Sense Dashboard<br/>
                <small style={{fontWeight: "normal"}}>Your sixth sense in developent, testing, and maintenance</small>
            </h3>
            <br/>
            <div className="aboutTable">
            {cells.map((row, rowIndex) => (<div className="aboutRow">{row.map((item, colIndex) => <span className={`aboutCell aboutCell${colIndex}`}>{item}</span>)}</div>))}
            </div>
            <br/>
                
                
                
{/*
            <p>API Url: &nbsp; {API_URL}</p>
            <p>API Version: &nbsp; {about.AppVersion}</p>
            <p>UI Version: &nbsp; {AppVersion.Version}</p>
            <p>Storage Server: &nbsp; {about.DbServer}</p>
            <p>Storage Database: &nbsp; {about.DbCatalog}</p>
*/}
        </Container> 
        
        
        </>;
    }

}