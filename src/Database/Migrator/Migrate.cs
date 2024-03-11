using FluentMigrator.Runner;
using Microsoft.Extensions.Options;

namespace LegalEntities.Database.Migrator;

public class MigrationRunner(IOptions<AppOptions> _options)
{
    public void MigrateUp()
    {
        var serviceProvider = new ServiceCollection()
            .AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                .AddPostgres()
                .WithGlobalConnectionString(_options.Value.DbConnectionString)
                .ScanIn(typeof(Program).Assembly).For.Migrations())
            .AddLogging(lb => lb.AddFluentMigratorConsole())
            .BuildServiceProvider(false);

        var migrationRunner = serviceProvider.GetRequiredService<IMigrationRunner>();
        migrationRunner.MigrateUp();
    }
}