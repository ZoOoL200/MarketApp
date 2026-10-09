using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReconcileStakeholderExpenseModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_BranchExpense_Category",
                table: "BranchExpenses");

            migrationBuilder.AddCheckConstraint(
                name: "CK_BranchExpense_Category",
                table: "BranchExpenses",
                sql: "\"Category\" IN ('Salary','Other')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_BranchExpense_Category",
                table: "BranchExpenses");

            migrationBuilder.AddCheckConstraint(
                name: "CK_BranchExpense_Category",
                table: "BranchExpenses",
                sql: "\"Category\" IN ('Salary','Rent','Other')");
        }
    }
}
