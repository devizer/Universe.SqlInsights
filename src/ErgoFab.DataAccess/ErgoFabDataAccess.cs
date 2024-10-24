using ErgoFab.Model;
using Microsoft.EntityFrameworkCore;

namespace ErgoFab.DataAccess;

public class ErgoFabDataAccess
{
    public readonly ErgoFabDbContext Db;

    public ErgoFabDataAccess(ErgoFabDbContext db)
    {
        Db = db;
    }

    public Organization GetOrganizationWithDepartmentsAndCountryFlag(int idOrganization)
    {
        return Db.Organization.AsNoTracking()
            .Include("Director")
            .Include("Country")
            .Include("Departments")
            .Include("Departments.Head")
            .FirstOrDefault(x => x.Id == idOrganization);
    }



    public List<Country> GetAllCountriesWithoutFlag()
    {
        IQueryable<Country> query =
            from x in Db.Country.AsNoTracking()
            orderby x.EnglishName
            select new Country
            {
                Id = x.Id,
                LocalName = x.LocalName,
                EnglishName = x.EnglishName
            };

        return query.ToList();
    }
}