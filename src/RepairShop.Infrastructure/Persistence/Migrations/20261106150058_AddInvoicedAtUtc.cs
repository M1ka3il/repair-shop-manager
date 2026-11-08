using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RepairShop.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Records when an order was invoiced and reports revenue by that date.
    /// <para>
    /// Before this, the revenue report assigned orders by <c>ReceivedAtUtc</c>:
    /// a car taken in on 31 October and billed on 6 November counted as
    /// October revenue, and "this month" showed nothing billed today.
    /// </para>
    /// <para>
    /// Existing invoiced orders get a best-effort date: their real invoice date
    /// was never stored. <c>CompletedAtUtc</c> is the closest known point (only
    /// completed work can be invoiced), <c>ReceivedAtUtc</c> the fallback.
    /// </para>
    /// </summary>
    public partial class AddInvoicedAtUtc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "InvoicedAtUtc",
                table: "RepairOrders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RepairOrders_Status_InvoicedAtUtc",
                table: "RepairOrders",
                columns: new[] { "Status", "InvoicedAtUtc" });

            // Backfill (Status 5 = Invoiced). An approximation, see the class summary.
            migrationBuilder.Sql("""
                UPDATE dbo.RepairOrders
                SET InvoicedAtUtc = COALESCE(CompletedAtUtc, ReceivedAtUtc)
                WHERE Status = 5 AND InvoicedAtUtc IS NULL;
                """);

            migrationBuilder.Sql(RevenueByInvoiceDate);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The previous procedure filtered by ReceivedAtUtc; restore it before
            // the column it would otherwise not need disappears.
            migrationBuilder.Sql(RevenueByReceiptDate);

            migrationBuilder.DropIndex(
                name: "IX_RepairOrders_Status_InvoicedAtUtc",
                table: "RepairOrders");

            migrationBuilder.DropColumn(
                name: "InvoicedAtUtc",
                table: "RepairOrders");
        }

        private const string RevenueByInvoiceDate = """
                CREATE OR ALTER PROCEDURE dbo.usp_GetRevenueByCustomer
                    @FromUtc DATETIME2,
                    @ToUtc   DATETIME2
                AS
                BEGIN
                    SET NOCOUNT ON;

                    -- Revenue per customer over invoiced orders only (Status 5 =
                    -- Invoiced), assigned to the period by invoice date: revenue belongs
                    -- to the day it was billed, not the day the car came in.
                    -- LEFT JOIN on the items so an invoiced order without lines
                    -- still shows up in the order count with 0.00 revenue.
                    SELECT
                        c.Id            AS CustomerId,
                        c.Name          AS CustomerName,
                        COUNT(DISTINCT o.Id) AS OrderCount,
                        CAST(ISNULL(SUM(CASE WHEN i.Type = 1 THEN i.UnitPrice * i.Quantity END), 0) AS DECIMAL(18, 2))
                            AS LabourRevenue,
                        CAST(ISNULL(SUM(CASE WHEN i.Type = 2 THEN i.UnitPrice * i.Quantity END), 0) AS DECIMAL(18, 2))
                            AS PartsRevenue,
                        CAST(ISNULL(SUM(i.UnitPrice * i.Quantity), 0) AS DECIMAL(18, 2))
                            AS TotalRevenue
                    FROM dbo.Customers      AS c
                    INNER JOIN dbo.Vehicles AS v ON v.CustomerId = c.Id
                    INNER JOIN dbo.RepairOrders AS o
                        ON o.VehicleId = v.Id
                        AND o.Status = 5
                        AND o.InvoicedAtUtc >= @FromUtc
                        AND o.InvoicedAtUtc <= @ToUtc
                    LEFT JOIN dbo.RepairItems AS i ON i.RepairOrderId = o.Id
                    GROUP BY c.Id, c.Name
                    ORDER BY TotalRevenue DESC, c.Name ASC;
                END
            """;

        private const string RevenueByReceiptDate = """
                CREATE OR ALTER PROCEDURE dbo.usp_GetRevenueByCustomer
                    @FromUtc DATETIME2,
                    @ToUtc   DATETIME2
                AS
                BEGIN
                    SET NOCOUNT ON;

                    -- Revenue per customer over invoiced orders only (Status 5 =
                    -- Invoiced). Work in progress is not revenue.
                    -- LEFT JOIN on the items so an invoiced order without lines
                    -- still shows up in the order count with 0.00 revenue.
                    SELECT
                        c.Id            AS CustomerId,
                        c.Name          AS CustomerName,
                        COUNT(DISTINCT o.Id) AS OrderCount,
                        CAST(ISNULL(SUM(CASE WHEN i.Type = 1 THEN i.UnitPrice * i.Quantity END), 0) AS DECIMAL(18, 2))
                            AS LabourRevenue,
                        CAST(ISNULL(SUM(CASE WHEN i.Type = 2 THEN i.UnitPrice * i.Quantity END), 0) AS DECIMAL(18, 2))
                            AS PartsRevenue,
                        CAST(ISNULL(SUM(i.UnitPrice * i.Quantity), 0) AS DECIMAL(18, 2))
                            AS TotalRevenue
                    FROM dbo.Customers      AS c
                    INNER JOIN dbo.Vehicles AS v ON v.CustomerId = c.Id
                    INNER JOIN dbo.RepairOrders AS o
                        ON o.VehicleId = v.Id
                        AND o.Status = 5
                        AND o.ReceivedAtUtc >= @FromUtc
                        AND o.ReceivedAtUtc <= @ToUtc
                    LEFT JOIN dbo.RepairItems AS i ON i.RepairOrderId = o.Id
                    GROUP BY c.Id, c.Name
                    ORDER BY TotalRevenue DESC, c.Name ASC;
                END
            """;
    }
}
