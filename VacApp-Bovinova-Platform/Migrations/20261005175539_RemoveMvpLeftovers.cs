using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VacApp_Bovinova_Platform.Migrations
{
    /// <summary>
    /// MVP cleanup: drops users."subscription-plan". The column was left NOT NULL
    /// without default after SubscriptionPlan was removed from User, so every
    /// sign-up failed ("Field 'subscription-plan' doesn't have a default value").
    ///
    /// The snapshot also drops the removed BCs (subscriptions, payments, staff,
    /// AI chats, additional-collar-requests), but their tables are intentionally
    /// NOT dropped here: nothing maps them anymore, and dropping them would
    /// irreversibly delete data in already-deployed databases.
    /// </summary>
    public partial class RemoveMvpLeftovers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "subscription-plan",
                table: "users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "subscription-plan",
                table: "users",
                type: "longtext",
                nullable: false,
                defaultValue: "Free");
        }
    }
}
