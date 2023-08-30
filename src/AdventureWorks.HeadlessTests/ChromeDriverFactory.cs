using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OpenQA.Selenium.Chrome;
using Universe;
using Universe.ChromeAndDriverInstaller;
using Universe.Shared;

namespace AdventureWorks.HeadlessTests
{
    public class ChromeDriverFactory
    {
        private static int Counter = 0;
        public static ChromeDriver Create()
        {
            string chromeDriverPath = null;
            var chromeMajorVersion = CurrentChromeVersionClient.TryGetMajorVersion();
            if (chromeMajorVersion.HasValue)
            {
                Console.WriteLine($"Local chrome detected: v{chromeMajorVersion}. Downloading chrome driver");
                var metadataClient = new SmartChromeAndDriverMetadataClient();
                var entries = metadataClient.Read();
                var driverEntry = entries?.Normalize().FindByVersion(chromeMajorVersion.Value);
                if (driverEntry != null)
                {
                    string tempDir;
                    if (TinyCrossInfo.IsWindows)
                        tempDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "chrome-drivers");
                    else
                        tempDir = "/tmp/chrome-drivers";

                    var counter = Interlocked.Increment(ref Counter);
                    tempDir = Path.Combine(tempDir, counter.ToString("0"));

                    var uniqueName = $"{driverEntry.Type}-{driverEntry.RawVersion}-{driverEntry.Platform}";
                    var zipFileName = Path.Combine(tempDir, "Zips", uniqueName + ".zip");
                    TryAndRetry.Exec(() => Directory.CreateDirectory(Path.GetDirectoryName(zipFileName)));
                    new WebDownloader().DownloadFile(driverEntry.Url, zipFileName, retryCount: 3);

                    var extractDir = Path.Combine(tempDir, uniqueName);
                    Console.WriteLine($"Extracting {driverEntry} into [{extractDir}]");
                    TryAndRetry.Exec(() => Directory.CreateDirectory(extractDir));
                    chromeDriverPath = ChromeOrDriverExtractor.Extract(driverEntry, zipFileName, extractDir);
                    Console.WriteLine($"ChromeDriver {driverEntry}: {Environment.NewLine}{chromeDriverPath}");

                }
            }

            if (chromeDriverPath != null)
            {
                ChromeDriverService svc = ChromeDriverService.CreateDefaultService(Path.GetDirectoryName(chromeDriverPath), Path.GetFileName(chromeDriverPath));
                return new ChromeDriver(svc);
            } 
            
            return new ChromeDriver();
        }
    }
}
