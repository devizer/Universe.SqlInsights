      set -eu; set -o pipefail
      try-and-retry curl -kfSL -o /tmp/chrome.deb https://dl.google.com/linux/direct/google-chrome-stable_current_amd64.deb
      sudo dpkg -i /tmp/chrome.deb
      sudo apt-get install -f -y
      printf "/usr/bin/env bash\n port=9234; google-chrome --headless --disable-gpu --no-first-run --no-sandbox \"\$@\"\n" | sudo tee /usr/local/bin/www-browser
      sudo chmod +x /usr/local/bin/www-browser
      for c in google-chrome www-browser; do
        echo "COMMAND '$c': [$(command -v $c)]"
      done


