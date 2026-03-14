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


            List<Country> countries = Enumerable.Range(1, 200).Select(x => new Country()
            {
                EnglishName = "Great Britain",
                LocalName = "Great Britain",
                Flag = UTF8Encoding.UTF8.GetBytes("Great Britain"),
            }).ToList();

            using (ErgoFabDbContext db0 = dbConnectionString.CreateErgoFabDbContext())
            {
                db0.Country.AddRange(countries);
                await db0.SaveChangesAsync();
            }



            while (organizationsCount > 0)
            {
                int count = await AddOrganizationChunk(dbConnectionString, countries);
                organizationsCount -= count;
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }

        }

        static async Task<int> AddOrganizationChunk(IDbConnectionString dbConnectionString, List<Country> countries)
        {
            Random random = new Random(1);
            await using ErgoFabDbContext db = dbConnectionString.CreateErgoFabDbContext();
            db.Country.AttachRange(countries);

            int count = 0;
            for (int i = 0; i < 1000; i++)
            {
                var org = new Organization()
                {
                    Title = $"MI-{Guid.NewGuid()}",
                    TheCountry = countries[random.Next(countries.Count)]
                };

                db.Organization.Add(org);
                count++;
            }
            await db.SaveChangesAsync();

            return count;
        }

    }
}
