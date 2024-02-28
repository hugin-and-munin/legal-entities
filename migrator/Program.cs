using System.CommandLine;
using LegalEntities.Migrator;

var connectionStringOption = new Option<string>(new[] { "-con", "--connection-string" }, "DB connection string");

var upCommand = new Command("up") { connectionStringOption };

upCommand.SetHandler(MigrationRunner.MigrateUp, connectionStringOption);
upCommand.Invoke(args);