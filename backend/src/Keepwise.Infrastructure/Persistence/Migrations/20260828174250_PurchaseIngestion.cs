using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Keepwise.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PurchaseIngestion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LifecycleStatus",
                table: "items",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerId",
                table: "attachments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OwnerType",
                table: "attachments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ingestion_jobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceType = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ContentType = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    StorageKey = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    ErrorCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    CandidateId = table.Column<Guid>(type: "uuid", nullable: true),
                    OcrRequests = table.Column<int>(type: "integer", nullable: false),
                    LlmRequests = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ingestion_jobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "purchase_candidates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceType = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Fingerprint = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DuplicateOfId = table.Column<Guid>(type: "uuid", nullable: true),
                    ConfirmedItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    OverallConfidence = table.Column<double>(type: "double precision", nullable: false),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_candidates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "purchases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    CandidateId = table.Column<Guid>(type: "uuid", nullable: true),
                    VendorName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    OrderNumber = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    InvoiceNumber = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    PurchasedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Gstin = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    UpiReference = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    ReturnBy = table.Column<DateOnly>(type: "date", nullable: true),
                    Fingerprint = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_purchases_items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_ingestion_settings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceiptScanningEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    SharedTextEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    EmailScanningEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    SmsImportEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    WhatsAppImportEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AiProcessingEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_ingestion_settings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_ingestion_settings_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ingestion_jobs_Sha256",
                table: "ingestion_jobs",
                column: "Sha256");

            migrationBuilder.CreateIndex(
                name: "IX_ingestion_jobs_UserId_Status",
                table: "ingestion_jobs",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_purchase_candidates_Sha256",
                table: "purchase_candidates",
                column: "Sha256");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_candidates_UserId_Fingerprint",
                table: "purchase_candidates",
                columns: new[] { "UserId", "Fingerprint" });

            migrationBuilder.CreateIndex(
                name: "IX_purchase_candidates_UserId_Status",
                table: "purchase_candidates",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_purchases_ItemId",
                table: "purchases",
                column: "ItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchases_UserId_Fingerprint",
                table: "purchases",
                columns: new[] { "UserId", "Fingerprint" });

            migrationBuilder.CreateIndex(
                name: "IX_user_ingestion_settings_UserId",
                table: "user_ingestion_settings",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ingestion_jobs");

            migrationBuilder.DropTable(
                name: "purchase_candidates");

            migrationBuilder.DropTable(
                name: "purchases");

            migrationBuilder.DropTable(
                name: "user_ingestion_settings");

            migrationBuilder.DropColumn(
                name: "LifecycleStatus",
                table: "items");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "attachments");

            migrationBuilder.DropColumn(
                name: "OwnerType",
                table: "attachments");
        }
    }
}
