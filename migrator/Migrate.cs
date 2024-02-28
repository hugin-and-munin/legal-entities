using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;

namespace LegalEntities.Migrator;

public static class MigrationRunner
{
    public static void MigrateUp(string connectionString)
    {
        var serviceProvider = new ServiceCollection()
            .AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                .AddPostgres()
                .WithGlobalConnectionString(connectionString)
                .ScanIn(typeof(Program).Assembly).For.Migrations())
            .AddLogging(lb => lb.AddFluentMigratorConsole())
            .BuildServiceProvider(false);

        var migrationRunner = serviceProvider.GetRequiredService<IMigrationRunner>();
        migrationRunner.MigrateUp();
    }
}