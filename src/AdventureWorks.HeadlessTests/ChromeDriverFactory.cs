using System;
using System.IO;
using System.Threading;
using OpenQA.Selenium.Chrome;
using Universe;
using Universe.ChromeAndDriverInstaller;
using Universe.Shared;

namespace AdventureWorks.HeadlessTests
{
    public class ChromeDriverFactory
    {
        private static int Counter = 0;

        // private static Lazy<ChromeDriver> _CurrentChromeDriver = new Lazy<ChromeDriver>(Create, LazyThreadSafetyMode.ExecutionAndPublication);
        // public static ChromeDriver CurrentChromeDriver = _CurrentChromeDriver.Value;

        static readonly object WorkaroundSync = new object();

        static ChromeDriverFactory()
        {
            WorkaroundSync = new object();
        }


        public static ChromeDriver Create()
        {
            lock (WorkaroundSync)
            {
                int? currentMajor = CurrentChromeVersionClient.TryGetMajorVersion();
                Console.WriteLine($"Downloading chrome driver for the Current Chrome Version '{currentMajor}'");
                var driverResult = ChromeOrDriverFactory.DownloadAndExtract(currentMajor, ChromeOrDriverType.Driver);
                if (driverResult != null)
                {
                    string chromeDriverPath = driverResult.ExecutableFullPath;
                    Console.WriteLine($"Have got chrome driver {driverResult.Metadata?.RawVersion} '{chromeDriverPath}'");
                    ChromeDriverService svc =
                        ChromeDriverService.CreateDefaultService(Path.GetDirectoryName(chromeDriverPath), Path.GetFileName(chromeDriverPath));
                    return new ChromeDriver(svc);
                }

                return new ChromeDriver();
            }
        }

        public static ChromeDriver Create_Prev()
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
