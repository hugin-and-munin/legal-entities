using FluentMigrator;

namespace LegalEntities.Database.Migrator.Migrations;

[Migration(20240201, "Creates LegalEntities table")]
public class CreateLegalEntitiesTable : Migration
{
    public override void Up()
    {
        Create.Table("LegalEntities")
            .WithColumn("Tin").AsInt64().PrimaryKey()
            .WithColumn("Json").AsString()
            .WithColumn("ReceivedAt").AsDateTimeOffset();
    }

    public override void Down()
    {
        Delete.Table("LegalEntities");
    }
}