Import-DevOps

Say "Installing chrome ..."
# choco install googlechrome
choco install chromium --version 74.0.3729.157


Say "CONTENT: C:\Program Files\Google\Chrome\Application"
Get-ChildItem "C:\Program Files\Google\Chrome\Application" | format-table -autosize

Say "Assing chrome for http://"
# reg add "HKEY_CLASSES_ROOT\http\shell\open\command" /ve /t REG_SZ /d "\"C:\Program Files\Google\Chrome\Application\chrome.exe\" --headless --no-sandbox --disable-gpu \"%%1\"" /f
& reg.exe add "HKEY_CLASSES_ROOT\http\shell\open\command" /ve /t REG_SZ /d '\"C:\Program Files\Google\Chrome\Application\chrome.exe\" --headless --no-sandbox --disable-gpu \"%1\"' /f

Say "Installing dotnet serve ..."
& dotnet tool install --global dotnet-serve

# ENV PATH="${PATH};C:\Users\ContainerUser\.dotnet\tools;C:\Program Files\Google\Chrome\Application"
# RUN reg add "HKEY_CLASSES_ROOT\http\shell\open\command" /ve /t REG_SZ /d "\"C:\Program Files\Google\Chrome\Application\chrome.exe\" --headless --no-sandbox --disable-gpu \"%%1\"" /f
