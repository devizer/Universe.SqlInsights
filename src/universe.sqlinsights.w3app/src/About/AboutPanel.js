import * as Helper from "../Helper"
import React, { Component } from 'react';
import {API_URL} from '../BuildTimeConfiguration';
import AppVersion from "../AppVersion.json"
import url from 'url';
import PropTypes from "prop-types";
import ReactComponentWithPerformance from "../Shared/ReactComponentWithPerformance";
import {ActionKeyPathUi} from "../Shared/ActionKeyPathUi";
import {Paper} from "@material-ui/core";

export default class AboutPanel extends Component {
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
        return <div className="padding-top center-aligned" style={{width: "100%"}}>
            <br/>    
            <div style={{display: "inline-block", width: 800, maxWidth: 800, minWidth: 800, padding: 24, border: "1px solid gray", textAlign: "center", boxShadow: "-1px -1px 7px 0px #989997, 1px 1px 2px 0px #CECECE" }}>
                <h3>SQL Server Sixth Sense Dashboard<br/><small style={{fontWeight: "normal"}}>Your sixth sense in developent, testing, and maintenance</small></h3>
                <div className="left-aligned padding-top">
                    <p>API Url: &nbsp; {API_URL}</p>
                    <p>API Version: &nbsp; {about.AppVersion}</p>
                    <p>UI Version: &nbsp; {AppVersion.Version}</p>
                    <p>Storage Server: &nbsp; {about.DbServer}</p>
                    <p>Storage Database: &nbsp; {about.DbCatalog}</p>
                </div>
            </div>
        </div>;
    }

}