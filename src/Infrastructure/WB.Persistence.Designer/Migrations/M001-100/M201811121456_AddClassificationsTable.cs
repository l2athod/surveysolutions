﻿using FluentMigrator;

namespace WB.UI.Designer.Migrations.PlainStore
{
    [Migration(201811121456)]
    public class M201811121456_AddClassificationsTable : Migration
    {
        private string tableName = "classificationentities";

        public override void Up()
        {
            if (!this.Schema.Table(tableName).Exists())
            {
                this.Create.Table(tableName)
                    .WithColumn("id").AsGuid().PrimaryKey()
                    .WithColumn("title").AsString()
                    .WithColumn("parent").AsGuid().Nullable()
                    .WithColumn("type").AsInt16()
                    .WithColumn("value").AsInt32().Nullable()
                    .WithColumn("index").AsInt32().Nullable()
                    .WithColumn("classificationid").AsGuid().Nullable()
                    .WithColumn("userid").AsGuid().Nullable();

                this.Create.Index($"parent_{tableName}_idx")
                    .OnTable(tableName)
                    .OnColumn("parent");

                this.Create.Index($"classificationid_{tableName}_idx")
                    .OnTable(tableName)
                    .OnColumn("classificationid");

                this.Create.Index($"userid_{tableName}_idx")
                    .OnTable(tableName)
                    .OnColumn("userid");
            }
        }

        public override void Down()
        {
            this.Delete.Table(tableName);
        }
    }
}
