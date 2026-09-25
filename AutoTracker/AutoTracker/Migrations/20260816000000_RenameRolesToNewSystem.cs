using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoTracker.Migrations
{
    /// <inheritdoc />
    public partial class RenameRolesToNewSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Owner and Admin → Administrator
            migrationBuilder.Sql("UPDATE AppUsers SET Role = 'Administrator' WHERE Role IN ('Owner', 'Admin')");

            // Manager → Workshop Manager
            migrationBuilder.Sql("UPDATE AppUsers SET Role = 'Workshop Manager' WHERE Role = 'Manager'");

            // Workshop User, Estimator, Read Only → Technician
            migrationBuilder.Sql("UPDATE AppUsers SET Role = 'Technician' WHERE Role IN ('Workshop User', 'Estimator', 'Read Only')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse: Administrator → Owner
            migrationBuilder.Sql("UPDATE AppUsers SET Role = 'Owner' WHERE Role = 'Administrator'");

            // Workshop Manager → Manager
            migrationBuilder.Sql("UPDATE AppUsers SET Role = 'Manager' WHERE Role = 'Workshop Manager'");

            // Technician → Workshop User
            migrationBuilder.Sql("UPDATE AppUsers SET Role = 'Workshop User' WHERE Role = 'Technician'");
        }
    }
}
