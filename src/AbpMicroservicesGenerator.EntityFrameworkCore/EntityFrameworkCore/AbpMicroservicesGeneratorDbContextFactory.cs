using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace AbpMicroservicesGenerator.EntityFrameworkCore;

/* This class is needed for EF Core console commands
 * (like Add-Migration and Update-Database commands) */
public class AbpMicroservicesGeneratorDbContextFactory : IDesignTimeDbContextFactory<AbpMicroservicesGeneratorDbContext>
{
    public AbpMicroservicesGeneratorDbContext CreateDbContext(string[] args)
    {
        AbpMicroservicesGeneratorEfCoreEntityExtensionMappings.Configure();

        var configuration = BuildConfiguration();

        var builder = new DbContextOptionsBuilder<AbpMicroservicesGeneratorDbContext>()
            .UseSqlServer(configuration.GetConnectionString("Default"));

        return new AbpMicroservicesGeneratorDbContext(builder.Options);
    }

    private static IConfigurationRoot BuildConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../AbpMicroservicesGenerator.DbMigrator/"))
            .AddJsonFile("appsettings.json", optional: false);

        return builder.Build();
    }
}
