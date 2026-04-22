using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoiceConcierge.Migrations
{
    /// <inheritdoc />
    public partial class RawKnowledgeEmbeddings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RawPropertyRecordId",
                table: "knowledge_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "knowledge_embeddings",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()");

            migrationBuilder.AddColumn<string>(
                name: "EmbeddingModel",
                table: "knowledge_embeddings",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VectorJson",
                table: "knowledge_embeddings",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "raw_property_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Section = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SubTitleLabel = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SubTitleValue = table.Column<string>(type: "text", nullable: true),
                    DetailsLabel = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DetailsValue = table.Column<string>(type: "text", nullable: true),
                    SearchText = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_raw_property_records", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_knowledge_items_RawPropertyRecordId",
                table: "knowledge_items",
                column: "RawPropertyRecordId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_raw_property_records_Section_Title",
                table: "raw_property_records",
                columns: new[] { "Section", "Title" });

            migrationBuilder.AddForeignKey(
                name: "FK_knowledge_items_raw_property_records_RawPropertyRecordId",
                table: "knowledge_items",
                column: "RawPropertyRecordId",
                principalTable: "raw_property_records",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_knowledge_items_raw_property_records_RawPropertyRecordId",
                table: "knowledge_items");

            migrationBuilder.DropTable(
                name: "raw_property_records");

            migrationBuilder.DropIndex(
                name: "IX_knowledge_items_RawPropertyRecordId",
                table: "knowledge_items");

            migrationBuilder.DropColumn(
                name: "RawPropertyRecordId",
                table: "knowledge_items");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "knowledge_embeddings");

            migrationBuilder.DropColumn(
                name: "EmbeddingModel",
                table: "knowledge_embeddings");

            migrationBuilder.DropColumn(
                name: "VectorJson",
                table: "knowledge_embeddings");
        }
    }
}
