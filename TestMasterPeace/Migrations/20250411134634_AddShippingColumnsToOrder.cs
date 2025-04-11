using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TestMasterPeace.Migrations
{
    /// <inheritdoc />
    public partial class AddShippingColumnsToOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            /* // --- Comment out ALL CreateTable sections --- 
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Categori__3213E83FDE90880F", x => x.id);
                });
            
            migrationBuilder.CreateTable(
                name: "Partners",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    contact_info = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Partners__3213E83F32CF4D84", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    password = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    profileIMG = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    phoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Users__3213E83F20856BF4", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ContactUs",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<long>(type: "bigint", nullable: true),
                    message = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__ContactU__3213E83F7C0D2235", x => x.id);
                    table.ForeignKey(
                        name: "FK__ContactUs__user___0C50D423",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "id");
                });
            
            // We will modify Orders, not create it
            // migrationBuilder.CreateTable(
            //    name: "Orders", ... );
            
            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    price = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    category_id = table.Column<long>(type: "bigint", nullable: true),
                    seller_id = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    img = table.Column<string>(type: "varchar(max)", unicode: false, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Products__3213E83F1E3C4ED9", x => x.id);
                    table.ForeignKey(
                        name: "FK__Products__catego__01D345B0",
                        column: x => x.category_id,
                        principalTable: "Categories",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK__Products__seller__02C769E9",
                        column: x => x.seller_id,
                        principalTable: "Users",
                        principalColumn: "id");
                });
            
            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    order_id = table.Column<long>(type: "bigint", nullable: true),
                    payment_method = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    amount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    transaction_date = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Transact__3213E83F84653F3B", x => x.id);
                    table.ForeignKey(
                        name: "FK__Transacti__order__1E6F845E",
                        column: x => x.order_id,
                        principalTable: "Orders",
                        principalColumn: "id");
                });
                
            migrationBuilder.CreateTable(
                name: "Cart",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<long>(type: "bigint", nullable: true),
                    product_id = table.Column<long>(type: "bigint", nullable: true),
                    quantity = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Cart__3213E83FF3164D7F", x => x.id);
                    table.ForeignKey(
                        name: "FK__Cart__product_id__11158940",
                        column: x => x.product_id,
                        principalTable: "Products",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK__Cart__user_id__10216507",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "id");
                });
                
            migrationBuilder.CreateTable(
                name: "Feedback",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<long>(type: "bigint", nullable: true),
                    product_id = table.Column<long>(type: "bigint", nullable: true),
                    rating = table.Column<int>(type: "int", nullable: true),
                    comment = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Feedback__3213E83F66857FA9", x => x.id);
                    table.ForeignKey(
                        name: "FK__Feedback__produc__078C1F06",
                        column: x => x.product_id,
                        principalTable: "Products",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK__Feedback__user_i__0697FACD",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "OrderItems",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    order_id = table.Column<long>(type: "bigint", nullable: true),
                    product_id = table.Column<long>(type: "bigint", nullable: true),
                    quantity = table.Column<int>(type: "int", nullable: false),
                    price = table.Column<decimal>(type: "decimal(10,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__OrderIte__3213E83F43D561D3", x => x.id);
                    table.ForeignKey(
                        name: "FK__OrderItem__order__18B6AB08",
                        column: x => x.order_id,
                        principalTable: "Orders",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK__OrderItem__produ__19AACF41",
                        column: x => x.product_id,
                        principalTable: "Products",
                        principalColumn: "id");
                });
            --- End of commented out CreateTable sections --- */

            // --- Keep ONLY the AddColumn operations for Orders --- 
            migrationBuilder.AddColumn<string>(
                name: "ShippingAddressLine1",
                table: "Orders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: ""); // Default might be needed if rows exist

            migrationBuilder.AddColumn<string>(
                name: "ShippingAddressLine2",
                table: "Orders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingCity",
                table: "Orders",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: ""); // Default might be needed if rows exist

            migrationBuilder.AddColumn<string>(
                name: "ShippingPhoneNumber",
                table: "Orders",
                type: "nvarchar(15)",
                maxLength: 15,
                nullable: false,
                defaultValue: ""); // Default might be needed if rows exist
            // --- End of AddColumn operations --- 

            /* // --- Comment out CreateIndex if they refer to newly created tables --- 
            migrationBuilder.CreateIndex(...);
            migrationBuilder.CreateIndex(...);
            ... etc ...
            --- End of commented out CreateIndex --- */
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // --- Keep ONLY the DropColumn operations for Orders --- 
            migrationBuilder.DropColumn(
                name: "ShippingAddressLine1",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingAddressLine2",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingCity",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingPhoneNumber",
                table: "Orders");
            // --- End of DropColumn operations --- 

            /* // --- Comment out ALL DropTable sections --- 
            migrationBuilder.DropTable(
                name: "Cart");
            migrationBuilder.DropTable(
                name: "ContactUs");
             migrationBuilder.DropTable(
                name: "Feedback");
            migrationBuilder.DropTable(
                name: "OrderItems");
            migrationBuilder.DropTable(
                name: "Partners");
            migrationBuilder.DropTable(
                name: "Transactions");
             migrationBuilder.DropTable(
                name: "Products");
            // We modified Orders, do not drop it here
            // migrationBuilder.DropTable(
            //    name: "Orders"); 
            migrationBuilder.DropTable(
                name: "Categories");
            migrationBuilder.DropTable(
                name: "Users");
            // --- End of commented out DropTable sections --- */
        }
    }
}
