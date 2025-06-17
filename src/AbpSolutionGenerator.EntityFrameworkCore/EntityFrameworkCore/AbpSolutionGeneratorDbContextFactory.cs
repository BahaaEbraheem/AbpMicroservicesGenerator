using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace AbpSolutionGenerator.EntityFrameworkCore;

/* This class is needed for EF Core console commands
 * (like Add-Migration and Update-Database commands) */
public class AbpSolutionGeneratorDbContextFactory : IDesignTimeDbContextFactory<AbpSolutionGeneratorDbContext>
{
    public AbpSolutionGeneratorDbContext CreateDbContext(string[] args)
    {
        AbpSolutionGeneratorEfCoreEntityExtensionMappings.Configure();

        var configuration = BuildConfiguration();

        var builder = new DbContextOptionsBuilder<AbpSolutionGeneratorDbContext>()
            .UseSqlServer(configuration.GetConnectionString("Default"));

        return new AbpSolutionGeneratorDbContext(builder.Options);
    }

    private static IConfigurationRoot BuildConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../AbpSolutionGenerator.DbMigrator/"))
            .AddJsonFile("appsettings.json", optional: false);

        return builder.Build();
    }
}
