#!/usr/bin/env bash
set -e
set -u
set -o pipefail

function say() { 
   NC='\033[0m' Color_Green='\033[1;32m' Color_Red='\033[1;31m' Color_Yellow='\033[1;33m'; 
   local var="Color_${1:-}"
   local color="${!var}"
   shift 
   printf "${color:-}$*${NC}\n";
}

function find_decompressor() {
  COMPRESSOR_EXT=""
  COMPRESSOR_EXTRACT=""
  if [[ -n "${GCC_FORCE_GZIP_PRIORITY:-}" ]]; then
    if [[ "$(command -v gzip)" != "" ]]; then
      COMPRESSOR_EXT=gz
      COMPRESSOR_EXTRACT="gzip -f -d"
    elif [[ "$(command -v xz)" != "" ]]; then
      COMPRESSOR_EXT=xz
      COMPRESSOR_EXTRACT="xz -f -d"
    fi
  else
    if [[ "$(command -v xz)" != "" ]]; then
      COMPRESSOR_EXT=xz
      COMPRESSOR_EXTRACT="xz -f -d"
    elif [[ "$(command -v gzip)" != "" ]]; then
      COMPRESSOR_EXT=gz
      COMPRESSOR_EXTRACT="gzip -f -d"
    fi
  fi
}

function download_file_fialover() {
  local file="$1"
  shift
  for url in "$@"; do
    # DEBUG: echo -e "\nTRY: [$url] for [$file]"
    local err=0;
    download_file "$url" "$file" || err=$?
    # DEBUG: say Green "Download status for [$url] is [$err]"
    if [ "$err" -eq 0 ]; then return; fi
  done
  return 55;
}

try_count=0
function download_file() {
  local url="$1"
  local file="$2";
  local progress1="" progress2="" progress3="" 
  if [[ "${DOWNLOAD_SHOW_PROGRESS:-}" != "True" ]] || [[ ! -t 1 ]]; then
    progress1="-q -nv"       # wget
    progress2="-s"           # curl
    progress3="--quiet=true" # aria2c
  fi
  rm -f "$file" 2>/dev/null || rm -f "$file" 2>/dev/null || rm -f "$file"
  local try1=""
  if [[ -z "${SKIP_ARIA:-}" ]] && [[ "$(command -v aria2c)" != "" ]]; then
    [[ -n "${try1:-}" ]] && try1="$try1 || "
    try1="aria2c $progress3 --allow-overwrite=true --check-certificate=false -s 9 -x 9 -k 1M -j 9 -d '$(dirname "$file")' -o '$(basename "$file")' '$url'"
  fi
  if [[ -z "${SKIP_CURL:-}" ]] && [[ "$(command -v curl)" != "" ]]; then
    [[ -n "${try1:-}" ]] && try1="$try1 || "
    try1="${try1:-} curl $progress2 -f -kSL -o '$file' '$url'"
  fi
  if [[ -z "${SKIP_WGET:-}" ]] && [[ "$(command -v wget)" != "" ]]; then
    [[ -n "${try1:-}" ]] && try1="$try1 || "
    try1="${try1:-} wget $progress1 --no-check-certificate -O '$file' '$url'"
  fi
  if [[ "${try1:-}" == "" ]]; then
    echo "error: niether curl, wget or aria2c is available"
    exit 42;
  fi
  local i=0;
  rm -f "$file" 2>/dev/null || true
  for i in 1 2 3; do
    try_count=$((try_count+1))
    if [[ "${try_count}" -gt 1 ]]; then
      say Red "Try #${try_count} to download $url"
    fi
     local err=""
     # say Green "$try1"
     eval $try1 || err=1
     if [[ -z "$err" ]]; then return; fi
  done
  return 55;
}

machine="$(uname -m || true)"; machine="${machine:-unknown}"
rid=unknown
if [[ "$machine" == armv7* ]]; then
  rid=linux-arm;
elif [[ "$machine" == aarch64 || "$machine" == armv8* || "$machine" == arm64* ]]; then
  rid=linux-arm64;
  if [[ "$(getconf LONG_BIT)" == "32" ]]; then 
    rid=linux-arm; 
  fi
elif [[ "$machine" == x86_64 || "$machine" == amd64 ]]; then
  rid=linux-x64;
fi;
if [[ $(uname -s) == Darwin ]]; then
  rid=osx-x64;
  # TODO: arm
fi;
if [ -e /etc/os-release ]; then
  . /etc/os-release
  if [[ "${ID:-}" == "alpine" ]]; then
    rid=linux-musl-x64;
  fi
elif [ -e /etc/redhat-release ]; then
  redhatRelease=$(</etc/redhat-release)
  if [[ $redhatRelease == "CentOS release 6."* || $redhatRelease == "Red Hat Enterprise Linux Server release 6."* ]]; then
    rid=rhel.6-x64;
  fi
fi


find_decompressor
HTTP_PORT="${HTTP_PORT:-40080}"
HTTP_HOST="${HTTP_HOST:-0.0.0.0}"
ASPNETCORE_URLS="http://${HTTP_HOST}:${HTTP_PORT}" 
INSTALL_DIR="${INSTALL_DIR:-/opt/s4dashboard}"

# TODO
export DB_CONNECTION_STRING="${DB_CONNECTION_STRING:-Data Source=192.168.2.42; Initial Catalog=SqlInsights Local Warehouse; User ID=sa; password=\`1qazxsw2;TrustServerCertificate=True;Encrypt=False}"

file="sqlinsights-dashboard-$rid.tar.${COMPRESSOR_EXT}"
url="https://github.com/devizer/Universe.SqlInsights/releases/latest/download/$file"
if [[ -z "${HOME:-}" ]]; then copy=/tmp/$file; else copy=$HOME/$file; fi

say Green "Listening on: $ASPNETCORE_URLS"
say Green "Download url: $url"
say Green "Temporary download file: $copy"
say Green "Storage: $DB_CONNECTION_STRING"

download_file "$url" "$copy"

sudo mkdir -p "$INSTALL_DIR"
sudo rm -rf "$INSTALL_DIR/*"
pushd "$INSTALL_DIR" >/dev/null
if [[ ! -z "$(command -v pv)" ]]; then
  pv "$copy" | eval $COMPRESSOR_EXTRACT | sudo tar xf - 2>&1 | { grep -v "implausibly old time stamp" || true; } | { grep -v "in the future" || true; }
else
  cat "$copy" | eval $COMPRESSOR_EXTRACT | sudo tar xf - 2>&1 | { grep -v "implausibly old time stamp" || true; } | { grep -v "in the future" || true; }
fi
sudo rm -f "$copy"
popd >/dev/null
DB_CONNECTION_STRING_Escaped=$(systemd-escape "${DB_CONNECTION_STRING:-}")

sudo systemctl disable s4dashboard.service 2>/dev/null 1>&2 || true

echo '
[Unit]
Description=S4 Dashboard
After=network.target

[Service]
Type=simple
# PIDFile=/var/run/s4dashboard.pid
WorkingDirectory='$INSTALL_DIR'
ExecStart='$INSTALL_DIR'/Universe.SqlInsights.W3Api
Restart=on-failure
RestartSec=2
KillSignal=SIGINT
ExecStopPost='${ExecStopPost:-}'
SyslogIdentifier=s4dashboard
User=root
Environment=PID_FILE_FULL_PATH=/var/run/s4dashboard.pid
Environment=ASPNETCORE_URLS='$ASPNETCORE_URLS'
Environment=RESPONSE_COMPRESSION='${RESPONSE_COMPRESSION:-True}'
Environment=FORCE_HTTPS_REDIRECT=False
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false
Environment=ConnectionStrings__SqlInsights='$DB_CONNECTION_STRING_Escaped'

[Install]
WantedBy=multi-user.target
' | sudo tee /etc/systemd/system/s4dashboard.service >/dev/null

sudo systemctl enable s4dashboard.service
sudo systemctl daemon-reload || true
sudo systemctl start s4dashboard.service

sudo journalctl -fu s4dashboard.service
