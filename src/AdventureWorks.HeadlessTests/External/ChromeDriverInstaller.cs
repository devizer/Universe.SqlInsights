﻿using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace AdventureWorks.HeadlessTests.External
{
    public class ChromeDriverInstaller
    {
        private static readonly HttpClient httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://chromedriver.storage.googleapis.com/")
        };

        public Task Install() => Install(null, false);
        public Task Install(string chromeVersion) => Install(chromeVersion, false);
        public Task Install(bool forceDownload) => Install(null, forceDownload);

        public async Task Install(string chromeVersion, bool forceDownload)
        {
            // Instructions from https://chromedriver.chromium.org/downloads/version-selection
            //   First, find out which version of Chrome you are using. Let's say you have Chrome 72.0.3626.81.
            if (chromeVersion == null)
            {
                chromeVersion = GetChromeVersion();
            }

            //   Take the Chrome version number, remove the last part, 
            // chromeVersion = chromeVersion.Substring(0, chromeVersion.LastIndexOf('.'));
            chromeVersion = chromeVersion.Split('.').FirstOrDefault();


            //   and append the result to URL "https://chromedriver.storage.googleapis.com/LATEST_RELEASE_". 
            //   For example, with Chrome version 72.0.3626.81, you'd get a URL "https://chromedriver.storage.googleapis.com/LATEST_RELEASE_72.0.3626".
            var chromeDriverVersionResponse = await httpClient.GetAsync($"LATEST_RELEASE_{chromeVersion}");
            if (!chromeDriverVersionResponse.IsSuccessStatusCode)
            {
                if (chromeDriverVersionResponse.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new Exception($"ChromeDriver version not found for Chrome version {chromeVersion}");
                }
                else
                {
                    throw new Exception($"ChromeDriver version request failed with status code: {chromeDriverVersionResponse.StatusCode}, reason phrase: {chromeDriverVersionResponse.ReasonPhrase}");
                }
            }

            var chromeDriverVersion = await chromeDriverVersionResponse.Content.ReadAsStringAsync();

            string zipName = "chromedriver_win32.zip";
            string driverName = "chromedriver.exe";

            string targetPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            targetPath = Path.Combine(targetPath, driverName);
            if (!forceDownload && File.Exists(targetPath))
            {
                using (var process = Process.Start(
                           new ProcessStartInfo
                           {
                               FileName = targetPath,
                               Arguments = "--version",
                               UseShellExecute = false,
                               CreateNoWindow = true,
                               RedirectStandardOutput = true,
                               RedirectStandardError = true,
                           }
                       ))
                {
                    string existingChromeDriverVersion = await process.StandardOutput.ReadToEndAsync();
                    string error = await process.StandardError.ReadToEndAsync();
                    process.WaitForExit();
                    process.Kill();

                    // expected output is something like "ChromeDriver 88.0.4324.96 (68dba2d8a0b149a1d3afac56fa74648032bcf46b-refs/branch-heads/4324@{#1784})"
                    // the following line will extract the version number and leave the rest
                    existingChromeDriverVersion = existingChromeDriverVersion.Split(' ')[1];
                    if (chromeDriverVersion == existingChromeDriverVersion)
                    {
                        return;
                    }

                    if (!string.IsNullOrEmpty(error))
                    {
                        throw new Exception($"Failed to execute {driverName} --version");
                    }
                }
            }

            //   Use the URL created in the last step to retrieve a small file containing the version of ChromeDriver to use. For example, the above URL will get your a file containing "72.0.3626.69". (The actual number may change in the future, of course.)
            //   Use the version number retrieved from the previous step to construct the URL to download ChromeDriver. With version 72.0.3626.69, the URL would be "https://chromedriver.storage.googleapis.com/index.html?path=72.0.3626.69/".
            var driverZipResponse = await httpClient.GetAsync($"{chromeDriverVersion}/{zipName}");
            if (!driverZipResponse.IsSuccessStatusCode)
            {
                throw new Exception($"ChromeDriver download request failed with status code: {driverZipResponse.StatusCode}, reason phrase: {driverZipResponse.ReasonPhrase}");
            }

            // this reads the zipfile as a stream, opens the archive, 
            // and extracts the chromedriver executable to the targetPath without saving any intermediate files to disk
            using (var zipFileStream = await driverZipResponse.Content.ReadAsStreamAsync())
            using (var zipArchive = new ZipArchive(zipFileStream, ZipArchiveMode.Read))
            using (var chromeDriverWriter = new FileStream(targetPath, FileMode.Create))
            {
                var entry = zipArchive.GetEntry(driverName);
                using (Stream chromeDriverStream = entry.Open())
                {
                    await chromeDriverStream.CopyToAsync(chromeDriverWriter);
                }
            }
        }

        public string GetChromeVersion()
        {
            string chromePath = (string)Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\App Paths\\chrome.exe", null, null);
            if (chromePath == null)
            {
                throw new Exception("Google Chrome not found in registry");
            }

            var fileVersionInfo = FileVersionInfo.GetVersionInfo(chromePath);
            return fileVersionInfo.FileVersion;
        }
    }
}