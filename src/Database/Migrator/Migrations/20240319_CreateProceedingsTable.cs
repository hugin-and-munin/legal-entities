using FluentMigrator;

namespace LegalEntities.Database.Migrator.Migrations;

[Migration(20240319, "Creates Proceedings table")]
public class CreateProceedingsTable : Migration
{
    public override void Up()
    {
        Create.Table("Proceedings")
            .WithColumn("Tin").AsInt64().PrimaryKey()
            .WithColumn("Json").AsString()
            .WithColumn("ReceivedAt").AsDateTimeOffset();
    }

    public override void Down()
    {
        Delete.Table("Proceedings");
    }
}
