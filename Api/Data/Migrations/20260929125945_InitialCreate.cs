using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: true),
                    SecurityStamp = table.Column<string>(type: "TEXT", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "TEXT", nullable: true),
                    PhoneNumber = table.Column<string>(type: "TEXT", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RevokedTokens",
                columns: table => new
                {
                    JwtId = table.Column<string>(type: "TEXT", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RevokedTokens", x => x.JwtId);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RoleId = table.Column<string>(type: "TEXT", nullable: false),
                    ClaimType = table.Column<string>(type: "TEXT", nullable: true),
                    ClaimValue = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    ClaimType = table.Column<string>(type: "TEXT", nullable: true),
                    ClaimValue = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "TEXT", nullable: false),
                    ProviderKey = table.Column<string>(type: "TEXT", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "TEXT", nullable: true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    RoleId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    LoginProvider = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FormSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SubmittedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FormSubmissions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FormData",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TaxpayerFirstName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    TaxpayerLastName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    TaxpayerSsn = table.Column<string>(type: "TEXT", maxLength: 11, nullable: false),
                    SpouseFirstName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    SpouseLastName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    SpouseSsn = table.Column<string>(type: "TEXT", maxLength: 11, nullable: true),
                    AddressLine1 = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    AddressLine2 = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    City = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    State = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ZipCode = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    ForeignCountry = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ForeignProvince = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ForeignPostalCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    FilingStatus = table.Column<int>(type: "INTEGER", nullable: true),
                    DigitalAssetsQuestionYes = table.Column<bool>(type: "INTEGER", nullable: false),
                    MoreThanFourDependents = table.Column<bool>(type: "INTEGER", nullable: false),
                    Line1a = table.Column<decimal>(type: "TEXT", nullable: true),
                    Line1b = table.Column<decimal>(type: "TEXT", nullable: true),
                    Line1c = table.Column<decimal>(type: "TEXT", nullable: true),
                    Line1d = table.Column<decimal>(type: "TEXT", nullable: true),
                    Line1e = table.Column<decimal>(type: "TEXT", nullable: true),
                    Line1f = table.Column<decimal>(type: "TEXT", nullable: true),
                    Line1g = table.Column<decimal>(type: "TEXT", nullable: true),
                    Line1h = table.Column<decimal>(type: "TEXT", nullable: true),
                    Line1i = table.Column<decimal>(type: "TEXT", nullable: true),
                    Line1z = table.Column<decimal>(type: "TEXT", nullable: false),
                    Line2a = table.Column<decimal>(type: "TEXT", nullable: true),
                    Line2b = table.Column<decimal>(type: "TEXT", nullable: true),
                    Line3a = table.Column<decimal>(type: "TEXT", nullable: true),
                    Line3b = table.Column<decimal>(type: "TEXT", nullable: true),
                    Line4a = table.Column<decimal>(type: "TEXT", nullable: true),
                    Line4b = table.Column<decimal>(type: "TEXT", nullable: true),
                    Line5a = table.Column<decimal>(type: "TEXT", nullable: true),
                    Line5b = table.Column<decimal>(type: "TEXT", nullable: true),
                    Line6a = table.Column<decimal>(type: "TEXT", nullable: true),
                    Line6b = table.Column<decimal>(type: "TEXT", nullable: true),
                    Line7a = table.Column<decimal>(type: "TEXT", nullable: true),
                    Line8 = table.Column<decimal>(type: "TEXT", nullable: true),
                    Line9 = table.Column<decimal>(type: "TEXT", nullable: false),
                    Line10 = table.Column<decimal>(type: "TEXT", nullable: true),
                    Line11a = table.Column<decimal>(type: "TEXT", nullable: false),
                    Line11b = table.Column<decimal>(type: "TEXT", nullable: true),
                    Line12 = table.Column<decimal>(type: "TEXT", nullable: true),
                    Line13a = table.Column<decimal>(type: "TEXT", nullable: true),
                    Line13b = table.Column<decimal>(type: "TEXT", nullable: true),
                    Line14 = table.Column<decimal>(type: "TEXT", nullable: true),
                    Line15 = table.Column<decimal>(type: "TEXT", nullable: false),
                    Line16 = table.Column<decimal>(type: "TEXT", nullable: true),
                    Line17 = table.Column<decimal>(type: "TEXT", nullable: true),
                    Line18 = table.Column<decimal>(type: "TEXT", nullable: true),
                    Line19 = table.Column<decimal>(type: "TEXT", nullable: true),
                    Line20 = table.Column<decimal>(type: "TEXT", nullable: true),
                    Line21 = table.Column<decimal>(type: "TEXT", nullable: true),
                    Line22 = table.Column<decimal>(type: "TEXT", nullable: true),
                    Line23 = table.Column<decimal>(type: "TEXT", nullable: true),
                    Line24 = table.Column<decimal>(type: "TEXT", nullable: false),
                    Line25a = table.Column<decimal>(type: "TEXT", nullable: true),
                    Line25b = table.Column<decimal>(type: "TEXT", nullable: true),
                    Line25c = table.Column<decimal>(type: "TEXT", nullable: true),
                    Line25d = table.Column<decimal>(type: "TEXT", nullable: true),
                    Line26 = table.Column<decimal>(type: "TEXT", nullable: true),
                    Line27 = table.Column<decimal>(type: "TEXT", nullable: true),
                    Line28 = table.Column<decimal>(type: "TEXT", nullable: true),
                    Line29 = table.Column<decimal>(type: "TEXT", nullable: true),
                    Line30 = table.Column<decimal>(type: "TEXT", nullable: true),
                    Line31 = table.Column<decimal>(type: "TEXT", nullable: true),
                    Line32 = table.Column<decimal>(type: "TEXT", nullable: true),
                    Line33 = table.Column<decimal>(type: "TEXT", nullable: false),
                    Line34 = table.Column<decimal>(type: "TEXT", nullable: false),
                    Line35a = table.Column<decimal>(type: "TEXT", nullable: true),
                    RoutingNumber = table.Column<string>(type: "TEXT", nullable: true),
                    BankAccountType = table.Column<int>(type: "INTEGER", nullable: true),
                    AccountNumber = table.Column<string>(type: "TEXT", nullable: true),
                    Line36 = table.Column<decimal>(type: "TEXT", nullable: true),
                    Line37 = table.Column<decimal>(type: "TEXT", nullable: false),
                    Line38 = table.Column<decimal>(type: "TEXT", nullable: true),
                    ThirdPartyDesignee = table.Column<bool>(type: "INTEGER", nullable: false),
                    DesigneeName = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    DesigneePhone = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    DesigneePin = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    TaxpayerOccupation = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    TaxpayerIdentityProtectionPin = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    TaxpayerSignatureDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    SpouseOccupation = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    SpouseIdentityProtectionPin = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    SpouseSignatureDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    PreparerName = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    PreparerPhone = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    PreparerPtin = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    PreparerFirmName = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    PreparerFirmAddress = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    PreparerFirmEin = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    PreparerSelfEmployed = table.Column<bool>(type: "INTEGER", nullable: false),
                    PreparerSignatureDate = table.Column<DateOnly>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FormData_FormSubmissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "FormSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GeneratedDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RelativePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Sha256 = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeneratedDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GeneratedDocuments_FormSubmissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "FormSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ValidationFindings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Severity = table.Column<int>(type: "INTEGER", nullable: false),
                    Field = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Message = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValidationFindings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ValidationFindings_FormSubmissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "FormSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Dependents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Form1040DataId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FirstName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Ssn = table.Column<string>(type: "TEXT", maxLength: 11, nullable: false),
                    Relationship = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    QualifiesForChildTaxCredit = table.Column<bool>(type: "INTEGER", nullable: false),
                    QualifiesForOtherDependentCredit = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dependents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Dependents_FormData_Form1040DataId",
                        column: x => x.Form1040DataId,
                        principalTable: "FormData",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Dependents_Form1040DataId",
                table: "Dependents",
                column: "Form1040DataId");

            migrationBuilder.CreateIndex(
                name: "IX_FormData_SubmissionId",
                table: "FormData",
                column: "SubmissionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FormSubmissions_UserId",
                table: "FormSubmissions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_GeneratedDocuments_SubmissionId",
                table: "GeneratedDocuments",
                column: "SubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_RevokedTokens_ExpiresAtUtc",
                table: "RevokedTokens",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ValidationFindings_SubmissionId",
                table: "ValidationFindings",
                column: "SubmissionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "Dependents");

            migrationBuilder.DropTable(
                name: "GeneratedDocuments");

            migrationBuilder.DropTable(
                name: "RevokedTokens");

            migrationBuilder.DropTable(
                name: "ValidationFindings");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "FormData");

            migrationBuilder.DropTable(
                name: "FormSubmissions");

            migrationBuilder.DropTable(
                name: "AspNetUsers");
        }
    }
}
