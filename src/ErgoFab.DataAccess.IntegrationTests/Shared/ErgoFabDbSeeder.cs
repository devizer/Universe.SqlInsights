using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ErgoFab.Model;
using Universe.NUnitPipeline.SqlServerDatabaseFactory;
using Universe.SqlInsights.NUnit;

namespace ErgoFab.DataAccess.IntegrationTests.Shared
{
    internal class ErgoFabDbSeeder
    {
        public static async Task Seed(IDbConnectionString dbConnectionString, int organizationsCount = 1)
        {
            ErgoFabDbContext db = dbConnectionString.CreateErgoFabDbContext();

            for (int i = 0; i < organizationsCount; i++)
            {
                var title = (char)((i / 26) + 48 + 6) + ((char)((i%26)+65)).ToString();
                var org = new Organization()
                {
                    Title = $"MI-{title}",
                    TheCountry = new Country()
                    {
                        EnglishName = "Great Britain",
                        LocalName = "Great Britain",
                        Flag = UTF8Encoding.UTF8.GetBytes("Great Britain"),
                    },
                };

                db.Organization.Add(org);
            }

            await db.SaveChangesAsync();
        }
    }
}
