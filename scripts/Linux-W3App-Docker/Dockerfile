FROM nginx:stable

# ENV SQL_INSIGHTS_W3API_URL

COPY build /usr/share/nginx/html
COPY config-w3app.sh /
# RUN echo "CONTENT of CONTAINER's [/usr/share/nginx/html] folder"; ls -laR /usr/share/nginx/html

CMD set -e; bash -e /config-w3app.sh; \
  compression="$(echo "${COMPRESSION:-}" | tr '[:lower:]' '[:upper:]')"; \
  if [ "$compression" = "ON" ] || [ "$compression" = "TRUE" ] || [ "$compression" = "1" ]; then \
    echo "GZIP Compression: On (experimental, by COMPRESSION env parameter)"; \
    sed -i 's/#gzip/gzip/g' /etc/nginx/nginx.conf; \
  else \
    echo "GZIP Compression: Off (experimental, by COMPRESSION env parameter)"; \
  fi; \
  nginx -g "daemon off;"

