using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ErgoFab.DataAccess.IntegrationTests.Library;
using ErgoFab.Model;

namespace ErgoFab.DataAccess.IntegrationTests.Shared
{
    internal class SimpleSeeder
    {
        public static async Task Seed(IDbConnectionString dbConnectionString, int organizationsCount = 1)
        {
            ErgoFabDbContext db = dbConnectionString.CreateErgoFabDbContext();

            var org = new Organization()
            {
                Title = "MI-7",
                Country = new Country()
                {
                    EnglishName = "Great Britain",
                    LocalName = "Great Britain",
                    Flag = UTF8Encoding.UTF8.GetBytes("Great Britain"),
                },
            };

            db.Organization.Add(org);
            await db.SaveChangesAsync();
        }
    }
}
