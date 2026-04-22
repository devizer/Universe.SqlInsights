      set -eu; set -o pipefail

# 79th:   360 Mb of RAM
# 147th: 1200 Mb of RAM
# 109th:  460 Mb of RAM
Install-Chrome() {
  local ver="${1:-stable}"
  local index
  if [[ "$ver" == "79" ]]; then index=706915;
  elif [[ "$ver" == "109" ]]; then
    if [[ "$(Get-OS-Platform)" == Windows ]]; then 
      index="1069273" # 109.0.5412.0
    else
      index="1070081" # 109.0.5414.0
    fi
  else
    index="$ver"
  fi
  local folder="${PUPPETEER_BROWSERS_ROOT:-}"
  if [[ "$folder" == "" ]]; then
    folder="$HOME/Browsers"
  fi
  Say "Installing chromium '$ver' build index '$index' into '$folder'"
  mkdir -p "$folder"
  pushd "$folder" >/dev/null
  local exists="$(find . -name 'chrome' | grep "$index" || true)"
  if [[ -n "$exists" ]]; then
     echo "CHROME ALREADY EXISTS: [$exists]"
  else
     npx -y @puppeteer/browsers install chromium@${index}
  fi
  popd >/dev/null
}
      
      Install-Chrome "109"

      # https://chromium.googlesource.com/chromium/src/+refs - find full version for 109: 109.0.5414.176 и 109.0.5414.118
      # https://versionhistory.googleapis.com/v1/chrome/platforms/all/channels/stable/versions

      # https://omahaproxy.appspot.com/revision.json?version=109.0.5414.176

      # 109.0.5414.174
      # 109 linux x64: 1070081, https://commondatastorage.googleapis.com/chromium-browser-snapshots/index.html?prefix=Linux_x64/1070081/
      # actual: 107.0.5304.0
      # npx -y @puppeteer/browsers install chromium@1047731 

      # Linux x64 109.0.5414.0
      # npx -y @puppeteer/browsers install chromium@1070081
      # npx -y @puppeteer/browsers install chrome@1070081
      # Windows x64
      # Version 109.0.5412.0 (Developer Build) (64-bit)
      # npx -y @puppeteer/browsers install chromium@1069273







      if [[ -z "$(command -v google-chrome)" ]]; then
          # try-and-retry sudo apt-get install xdg-utils -y -qq || true
          # try-and-retry sudo apt-get install fonts-liberation -y -qq || true
          Say "Missing google-chrome, downloading ..."
          try-and-retry curl -kfSL -o /tmp/chrome.deb https://dl.google.com/linux/direct/google-chrome-stable_current_amd64.deb
          sudo dpkg -i /tmp/chrome.deb || sudo apt-get install -f
          rm -f /tmp/chrome.deb
      fi

      printf "/usr/bin/env bash\n port=9234; google-chrome --headless --disable-gpu --no-first-run --no-sandbox \"\$@\"\n" | sudo tee /usr/local/bin/www-browser
      sudo chmod +x /usr/local/bin/www-browser
      for c in google-chrome www-browser; do
        echo "COMMAND '$c': [$(command -v $c)]"
      done


