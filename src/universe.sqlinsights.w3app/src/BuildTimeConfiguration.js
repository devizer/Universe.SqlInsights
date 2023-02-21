// export const API_URL="http://localhost:8776/SqlInsights";
// export const API_URL="http://localhost:56111/AppInsight"
const urlByEnv = process.env.REACT_APP_SQL_INSIGHTS_W3API_URL;
const devUrl = "http://localhost:50420/api/v1/SqlInsights";
export const API_URL = urlByEnv ? urlByEnv : devUrl;
