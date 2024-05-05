using FluentMigrator;

namespace LegalEntities.Database.Migrator.Migrations;

[Migration(20240505, "Creates Financial Info table")]
public class CreateFinancialInfoTable : Migration
{
    public override void Up()
    {
        Create.Table("FinancialPeriods")
            .WithColumn("Tin").AsInt64().PrimaryKey()
            .WithColumn("Json").AsString()
            .WithColumn("ReceivedAt").AsDateTimeOffset();

        Create.Table("FinancialReports")
            .WithColumn("Tin").AsInt64().PrimaryKey()
            .WithColumn("Json").AsString()
            .WithColumn("ReceivedAt").AsDateTimeOffset();
    }

    public override void Down()
    {
        Delete.Table("FinancialReports");
        Delete.Table("FinancialPeriods");
    }
}
