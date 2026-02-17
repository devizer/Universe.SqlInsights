. "$PSScriptRoot\Functions.ps1"
if ((Get-OS-Platform) -eq "Windows") {
    & choco feature enable -n allowGlobalConfirmation
    & choco feature disable -n showDownloadProgress
}

Say "Installing chrome ..."
# choco install googlechrome --version 144.0.7559.97 --ignore-checksums
choco install chromium --version 74.0.3729.157 --ignore-checksums
# choco install chromium --version 144.0.7559.133 --ignore-checksums

Show-Chrome-Program-List

Say "Browsers by Installer"
Get-Speedy-Software-Product-List | ? { $_.Name -match "Chrome" -or $_.Name -match "Chromium" -or $_.Name -match "Microsoft Edge" -or $_.Name -match "FireFox" } | ft


# Say "Assing chrome for http://"
# reg add "HKEY_CLASSES_ROOT\http\shell\open\command" /ve /t REG_SZ /d "\"C:\Program Files\Google\Chrome\Application\chrome.exe\" --headless --no-sandbox --disable-gpu \"%%1\"" /f
# & reg.exe add "HKEY_CLASSES_ROOT\http\shell\open\command" /ve /t REG_SZ /d '\"C:\Program Files\Google\Chrome\Application\chrome.exe\" --headless --no-sandbox --disable-gpu \"%1\"' /f

# ENV PATH="${PATH};C:\Users\ContainerUser\.dotnet\tools;C:\Program Files\Google\Chrome\Application"
# RUN reg add "HKEY_CLASSES_ROOT\http\shell\open\command" /ve /t REG_SZ /d "\"C:\Program Files\Google\Chrome\Application\chrome.exe\" --headless --no-sandbox --disable-gpu \"%%1\"" /f
