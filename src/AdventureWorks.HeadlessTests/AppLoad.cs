using System.Collections.Generic;
using System.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace AdventureWorks.HeadlessTests
{
    public static class AppLoad
    {
        public static void RunCustomersLoop()
        {
            for (int i = 0; i <= 1; i++) RunCustomers();
        }

        public static void RunCustomers()
        {
            ChromeDriver driver = ChromeDriverFactory.Create();
            driver.Navigate().GoToUrl($"{Env.AppUrl}/Customer");

            List<LinkInfo> links = GetLinks(driver.FindElements(By.CssSelector("a.CustomerDetailsLink")).ToList());

            foreach (var linkInfo in links)
            {
                Logs.Info($"Opening client {linkInfo.Text}: {linkInfo.Link}");
                driver.Navigate().GoToUrl(linkInfo.Link);
                var charsCount = driver.FindElements(By.TagName("body")).FirstOrDefault()?.Text;
                Logs.Info($"Opened client  {linkInfo.Text}: {linkInfo.Link}, {charsCount.Length} chars");
                OpenOrderLinksOnCurrentPage(driver);
            }

            driver.Dispose();
        }
        public static void RunSalesOrders()
        {
            ChromeDriver driver = ChromeDriverFactory.Create();
            driver.Navigate().GoToUrl($"{Env.AppUrl}/SalesOrders");

            OpenOrderLinksOnCurrentPage(driver);

            // driver.Close();
            driver.Dispose();
        }

        static void OpenOrderLinksOnCurrentPage(WebDriver driver)
        {
            List<LinkInfo> linksOrders = GetLinks(driver.FindElements(By.CssSelector("a.OrderLink")).ToList());

            foreach (var linkOrder in linksOrders)
            {
                Logs.Info($"Opening order {linkOrder.Text}: {linkOrder.Link}");
                driver.Navigate().GoToUrl(linkOrder.Link);
                var charsCount = driver.FindElements(By.TagName("body")).FirstOrDefault()?.Text;
                Logs.Info($"Opened order  {linkOrder.Text}: {linkOrder.Link}, {charsCount.Length} chars");
            }
        }

        public static List<LinkInfo> GetLinks(IEnumerable<IWebElement> elements)
        {

            List<LinkInfo> links = elements
                .Select(x => new LinkInfo() { Link = x.GetAttribute("href"), Text = x.Text })
                .ToList();

            foreach (var link in links)
            {
                Logs.Info($"Link {link.Text}: {link.Link}");
            }

            return links;
        }

    }
}
