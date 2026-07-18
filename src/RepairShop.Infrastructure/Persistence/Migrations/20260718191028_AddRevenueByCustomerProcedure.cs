using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RepairShop.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Adds the revenue report as a stored procedure.
    /// <para>
    /// The procedure is versioned here, alongside the schema, rather than being
    /// applied by hand in SSMS — otherwise a fresh clone would come up with a
    /// database the API cannot query.
    /// </para>
    /// <para>
    /// CREATE OR ALTER makes the migration re-runnable against a database where
    /// an older version of the procedure already exists.
    /// </para>
    /// </summary>
    public partial class AddRevenueByCustomerProcedure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
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
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.usp_GetRevenueByCustomer;");
        }
    }
}
