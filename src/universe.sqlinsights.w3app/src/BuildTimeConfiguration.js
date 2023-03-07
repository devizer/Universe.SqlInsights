// export const API_URL="http://localhost:8776/SqlInsights";
// export const API_URL="http://localhost:56111/AppInsight"
const urlByEnv = process.env.REACT_APP_SQL_INSIGHTS_W3API_URL;
const urlBySrc = "http://localhost:50420/api/v1/SqlInsights";

let globalW3ApiUrl = global.GLOBAL_APP_CONFIGURATION?.SQL_INSIGHTS_W3API_URL;
console.log("%c Raw global.GLOBAL_APP_CONFIGURATION.SQL_INSIGHTS_W3API_URL: '%s'", 'color: white; background-color: black', globalW3ApiUrl);
if (globalW3ApiUrl === "SQL_INSIGHTS_W3API_URL_PLACEHOLDER") globalW3ApiUrl = null;  

export const API_URL = globalW3ApiUrl ? globalW3ApiUrl : urlByEnv ? urlByEnv : urlBySrc;

console.log("%c FINAL API_URL: '%s'", 'color: white; background-color: black', API_URL);
