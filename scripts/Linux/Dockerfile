FROM nginx:stable

# ENV SQL_INSIGHTS_W3API_URL

COPY build /usr/share/nginx/html
COPY config-w3app.sh /
# RUN echo "CONTENT of CONTAINER's [/usr/share/nginx/html] folder"; ls -laR /usr/share/nginx/html

CMD "bash -e /config-w3app.sh; nginx -g \"daemon off;\""
