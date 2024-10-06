using System;
using NUnit.Framework;
using Universe.NUnitTests;
using Universe.SqlInsights.Shared;

namespace Universe.SqlInsights.SqlServerStorage.Tests
{
    public class TestBothSerializers : NUnitTestsBase
    {
        [Test]
        public void TestSystemJsonOfActionDetails()
        {
            Assert.AreEqual("System", DbJsonConvert.Flawor);

            ActionDetailsWithCounters details = Seeder.CreateActionDetailsWithCounters(new SqlInsightsActionKeyPath("Topic", "Kind"));

            Console.WriteLine($"Original by Newtonsoft {Environment.NewLine}{DbJsonConvertLegacy.Serialize(details)}{Environment.NewLine}");
            Console.WriteLine($"Original by System {Environment.NewLine}{DbJsonConvert.Serialize(details)}{Environment.NewLine}");

            var detailsCopyLegacy = DbJsonConvertLegacy.Deserialize<ActionDetailsWithCounters>(DbJsonConvertLegacy.Serialize(details));
            Console.WriteLine($"Legacy Copy by Newtonsoft {Environment.NewLine}{DbJsonConvertLegacy.Serialize(detailsCopyLegacy)}{Environment.NewLine}");
            Console.WriteLine($"Legacy Copy by System {Environment.NewLine}{DbJsonConvert.Serialize(detailsCopyLegacy)}{Environment.NewLine}");
            
            var detailsCopySystem = DbJsonConvert.Deserialize<ActionDetailsWithCounters>(DbJsonConvert.Serialize(details));
            Console.WriteLine($"System Copy by Newtonsoft {Environment.NewLine}{DbJsonConvertLegacy.Serialize(detailsCopySystem)}{Environment.NewLine}");
            Console.WriteLine($"System Copy by System {Environment.NewLine}{DbJsonConvert.Serialize(detailsCopySystem)}{Environment.NewLine}");
            
            var detailsCopy = DbJsonConvert.Deserialize<ActionDetailsWithCounters>(DbJsonConvert.Serialize(details));
            Assert.AreEqual(DbJsonConvert.Serialize(details), DbJsonConvert.Serialize(detailsCopy), "Details by System Serializer");
            Assert.AreEqual(DbJsonConvertLegacy.Serialize(details), DbJsonConvertLegacy.Serialize(detailsCopy), "Details by Newtonsoft Serializer");

        }

        [Test]
        public static void TestSystemJsonOfActionSummary()
        {
            Assert.AreEqual("System", DbJsonConvert.Flawor);

            ActionDetailsWithCounters details = Seeder.CreateActionDetailsWithCounters(new SqlInsightsActionKeyPath("Topic", "Kind"));
            ActionSummaryCounters summary = details.AsSummary();
            ActionSummaryCounters summaryCopy = DbJsonConvert.Deserialize<ActionSummaryCounters>(DbJsonConvert.Serialize(summary));
            Assert.AreEqual(DbJsonConvert.Serialize(summary), DbJsonConvert.Serialize(summaryCopy), "Details by System Serializer");
            Assert.AreEqual(DbJsonConvertLegacy.Serialize(summary), DbJsonConvertLegacy.Serialize(summaryCopy), "Details by Newtonsoft Serializer");
        }

        
    }
}