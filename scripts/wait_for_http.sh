
function wait_for_http() {
  local startAt="$(date +%s)"
  u="$1"; t="${WAIT_HTTP_TIMEOUT:-30}"; 
  printf "Waiting for [$u] during $t seconds ..."
  while [ $t -ge 0 ]; do 
    t=$((t-1)); 
    e1=249;
    if [[ "$(command -v curl)" != "" ]]; then curl --connect-timeout 3 -skf "$u" >/dev/null; e1=$?; fi
    if [[ "$e1" -ne 0 ]]; then
      if [[ "$(command -v wget)" != "" ]]; then wget -q -nv -t 1 -T 3 "$u" >/dev/null; e1=$?; fi
    fi
    if [ "$e1" -eq 249 ]; then printf "MISSING wget|curl\n"; return; fi
    if [ "$e1" -eq 0 ]; then printf " OK\n"; return; fi; 
    printf ".";
    sleep 1;
    local now="$(date +%s)"; local seconds=$((now-startAt))
    if [ "$seconds" -gt "$t" ]; then break; fi
  done
  printf " FAIL\n";
}
