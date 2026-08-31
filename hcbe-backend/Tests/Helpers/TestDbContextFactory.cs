using Microsoft.EntityFrameworkCore;
using HcbeApi.Data;

namespace HcbeApi.Tests.Helpers;

public static class TestDbContextFactory
{
    public static ApplicationDbContext CreateInMemoryContext(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }
}

