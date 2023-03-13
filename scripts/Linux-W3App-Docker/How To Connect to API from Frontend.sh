docker rm -f dashboard-frontend; docker run --name dashboard-frontend -it --rm -e SQL_INSIGHTS_W3API_URL=http://jammy-arm64:8080/api/v1/SqlInsights -p 7070:80 devizervlad/sqlinsights-dashboard-frontend

docker network create sqlinsights
docker network connect sqlinsights dashboard
docker network connect sqlinsights dashboard-frontend

docker exec dashboard-frontend curl -I http://dashboard