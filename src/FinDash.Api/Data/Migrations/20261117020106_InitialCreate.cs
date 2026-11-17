using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FinDash.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Assets",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Symbol = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Kind = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    Source = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    BaseCurrency = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PriceSnapshots",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AssetId = table.Column<string>(type: "TEXT", nullable: false),
                    PriceUsd = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: false),
                    PriceCrc = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: false),
                    SellCrc = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: true),
                    Change24hPct = table.Column<decimal>(type: "TEXT", precision: 9, scale: 4, nullable: false),
                    CapturedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PriceSnapshots_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AlertEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AlertId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AssetId = table.Column<string>(type: "TEXT", nullable: false),
                    Condition = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
                    Threshold = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
                    PriceAtTrigger = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: false),
                    TriggeredAt = table.Column<long>(type: "INTEGER", nullable: false),
                    DeliveredAt = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlertEvents_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PriceAlerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AssetId = table.Column<string>(type: "TEXT", nullable: false),
                    Condition = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
                    Threshold = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    State = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    LastPrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: true),
                    LastTriggeredAt = table.Column<long>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceAlerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PriceAlerts_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PriceAlerts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AssetId = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
                    Quantity = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: false),
                    PriceCurrency = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
                    Fee = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FxRate = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Transactions_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Transactions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WatchlistItems",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AssetId = table.Column<string>(type: "TEXT", nullable: false),
                    AddedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WatchlistItems", x => new { x.UserId, x.AssetId });
                    table.ForeignKey(
                        name: "FK_WatchlistItems_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WatchlistItems_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Assets",
                columns: new[] { "Id", "Kind", "Name", "Source", "Symbol" },
                values: new object[,]
                {
                    { "avalanche-2", "Crypto", "Avalanche", "CoinGecko", "AVAX" },
                    { "binancecoin", "Crypto", "BNB", "CoinGecko", "BNB" },
                    { "bitcoin", "Crypto", "Bitcoin", "CoinGecko", "BTC" },
                    { "bitcoin-cash", "Crypto", "Bitcoin Cash", "CoinGecko", "BCH" },
                    { "cardano", "Crypto", "Cardano", "CoinGecko", "ADA" },
                    { "chainlink", "Crypto", "Chainlink", "CoinGecko", "LINK" },
                    { "dogecoin", "Crypto", "Dogecoin", "CoinGecko", "DOGE" },
                    { "ethereum", "Crypto", "Ethereum", "CoinGecko", "ETH" },
                    { "litecoin", "Crypto", "Litecoin", "CoinGecko", "LTC" },
                    { "polkadot", "Crypto", "Polkadot", "CoinGecko", "DOT" },
                    { "ripple", "Crypto", "XRP", "CoinGecko", "XRP" },
                    { "solana", "Crypto", "Solana", "CoinGecko", "SOL" },
                    { "stellar", "Crypto", "Stellar", "CoinGecko", "XLM" },
                    { "tether", "Crypto", "Tether", "CoinGecko", "USDT" },
                    { "tron", "Crypto", "TRON", "CoinGecko", "TRX" },
                    { "usd", "Fiat", "Dólar", "BCCR", "USD" },
                    { "usd-coin", "Crypto", "USD Coin", "CoinGecko", "USDC" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlertEvents_AlertId",
                table: "AlertEvents",
                column: "AlertId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertEvents_UserId_DeliveredAt",
                table: "AlertEvents",
                columns: new[] { "UserId", "DeliveredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PriceAlerts_AssetId",
                table: "PriceAlerts",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceAlerts_UserId",
                table: "PriceAlerts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceSnapshots_AssetId_CapturedAt",
                table: "PriceSnapshots",
                columns: new[] { "AssetId", "CapturedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_AssetId",
                table: "Transactions",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_UserId_AssetId",
                table: "Transactions",
                columns: new[] { "UserId", "AssetId" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WatchlistItems_AssetId",
                table: "WatchlistItems",
                column: "AssetId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlertEvents");

            migrationBuilder.DropTable(
                name: "PriceAlerts");

            migrationBuilder.DropTable(
                name: "PriceSnapshots");

            migrationBuilder.DropTable(
                name: "Transactions");

            migrationBuilder.DropTable(
                name: "WatchlistItems");

            migrationBuilder.DropTable(
                name: "Assets");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
