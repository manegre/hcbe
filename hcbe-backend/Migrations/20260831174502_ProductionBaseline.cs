using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcbeApi.Migrations
{
    /// <inheritdoc />
    public partial class ProductionBaseline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Associations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    NameEn = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    DescriptionEn = table.Column<string>(type: "text", nullable: true),
                    Province = table.Column<string>(type: "text", nullable: false),
                    City = table.Column<string>(type: "text", nullable: false),
                    Contact = table.Column<string>(type: "text", nullable: true),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    President = table.Column<string>(type: "text", nullable: true),
                    MemberCount = table.Column<string>(type: "text", nullable: true),
                    FoundedYear = table.Column<int>(type: "integer", nullable: true),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    Website = table.Column<string>(type: "text", nullable: true),
                    Domains = table.Column<string>(type: "text", nullable: false),
                    DomainsEn = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Associations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Consultations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    TitleEn = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: false),
                    DescriptionEn = table.Column<string>(type: "text", nullable: true),
                    Icon = table.Column<string>(type: "text", nullable: false),
                    LayoutType = table.Column<string>(type: "text", nullable: false),
                    ActionUrl = table.Column<string>(type: "text", nullable: true),
                    ActionLabel = table.Column<string>(type: "text", nullable: true),
                    ActionLabelEn = table.Column<string>(type: "text", nullable: true),
                    SecondaryActionUrl = table.Column<string>(type: "text", nullable: true),
                    SecondaryActionLabel = table.Column<string>(type: "text", nullable: true),
                    SecondaryActionLabelEn = table.Column<string>(type: "text", nullable: true),
                    AccentColor = table.Column<string>(type: "text", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Consultations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    NameEn = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    DescriptionEn = table.Column<string>(type: "text", nullable: true),
                    Icon = table.Column<string>(type: "text", nullable: true),
                    Type = table.Column<string>(type: "text", nullable: true),
                    Size = table.Column<string>(type: "text", nullable: true),
                    Pages = table.Column<string>(type: "text", nullable: true),
                    PagesEn = table.Column<string>(type: "text", nullable: true),
                    Category = table.Column<string>(type: "text", nullable: true),
                    CategoryEn = table.Column<string>(type: "text", nullable: true),
                    Url = table.Column<string>(type: "text", nullable: true),
                    Downloads = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    TitleEn = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    DescriptionEn = table.Column<string>(type: "text", nullable: true),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Location = table.Column<string>(type: "text", nullable: true),
                    LocationEn = table.Column<string>(type: "text", nullable: true),
                    Type = table.Column<string>(type: "text", nullable: true),
                    Zone = table.Column<string>(type: "text", nullable: true),
                    Capacity = table.Column<int>(type: "integer", nullable: true),
                    RegistrationDeadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MeetingLink = table.Column<string>(type: "text", nullable: true),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FooterLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    CategoryEn = table.Column<string>(type: "text", nullable: true),
                    Label = table.Column<string>(type: "text", nullable: false),
                    LabelEn = table.Column<string>(type: "text", nullable: true),
                    Url = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FooterLinks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GrantPrograms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    TitleEn = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: false),
                    DescriptionEn = table.Column<string>(type: "text", nullable: true),
                    Icon = table.Column<string>(type: "text", nullable: false),
                    Amount = table.Column<string>(type: "text", nullable: false),
                    AmountEn = table.Column<string>(type: "text", nullable: true),
                    Duration = table.Column<string>(type: "text", nullable: false),
                    DurationEn = table.Column<string>(type: "text", nullable: true),
                    EligibilityCriteria = table.Column<string>(type: "text", nullable: false),
                    EligibilityCriteriaEn = table.Column<string>(type: "text", nullable: false),
                    ApplicationUrl = table.Column<string>(type: "text", nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GrantPrograms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MemberProfiles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: false),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    Province = table.Column<string>(type: "text", nullable: true),
                    Zone = table.Column<string>(type: "text", nullable: true),
                    MembershipStatus = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Members",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    City = table.Column<string>(type: "text", nullable: true),
                    Province = table.Column<string>(type: "text", nullable: true),
                    Profession = table.Column<string>(type: "text", nullable: true),
                    Expertise = table.Column<string>(type: "text", nullable: true),
                    Interests = table.Column<string>(type: "text", nullable: true),
                    Availability = table.Column<string>(type: "text", nullable: true),
                    Zone = table.Column<string>(type: "text", nullable: true),
                    IsAdmin = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Members", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NavigationItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Label = table.Column<string>(type: "text", nullable: false),
                    LabelEn = table.Column<string>(type: "text", nullable: true),
                    Url = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NavigationItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "News",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    TitleEn = table.Column<string>(type: "text", nullable: true),
                    Content = table.Column<string>(type: "text", nullable: false),
                    ContentEn = table.Column<string>(type: "text", nullable: true),
                    Excerpt = table.Column<string>(type: "text", nullable: true),
                    ExcerptEn = table.Column<string>(type: "text", nullable: true),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    ImagePosition = table.Column<string>(type: "text", nullable: true),
                    Author = table.Column<string>(type: "text", nullable: true),
                    Category = table.Column<string>(type: "text", nullable: true),
                    PublishedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsPinned = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_News", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NewsletterCampaigns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Subject = table.Column<string>(type: "text", nullable: false),
                    SubjectEn = table.Column<string>(type: "text", nullable: true),
                    Body = table.Column<string>(type: "text", nullable: false),
                    BodyEn = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    RecipientCount = table.Column<int>(type: "integer", nullable: false),
                    SentCount = table.Column<int>(type: "integer", nullable: false),
                    FailedCount = table.Column<int>(type: "integer", nullable: false),
                    LastError = table.Column<string>(type: "text", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewsletterCampaigns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NewsletterSubscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    FullName = table.Column<string>(type: "text", nullable: false),
                    PreferredLanguage = table.Column<string>(type: "text", nullable: false),
                    ConsentAcceptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false),
                    UnsubscribeToken = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewsletterSubscriptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    RelatedEntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    Link = table.Column<string>(type: "text", nullable: true),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PageSections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Page = table.Column<string>(type: "text", nullable: false),
                    Section = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: true),
                    TitleEn = table.Column<string>(type: "text", nullable: true),
                    Content = table.Column<string>(type: "text", nullable: true),
                    ContentEn = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PageSections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Partners",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    NameEn = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    DescriptionEn = table.Column<string>(type: "text", nullable: true),
                    LogoUrl = table.Column<string>(type: "text", nullable: true),
                    WebsiteUrl = table.Column<string>(type: "text", nullable: true),
                    AltText = table.Column<string>(type: "text", nullable: true),
                    AltTextEn = table.Column<string>(type: "text", nullable: true),
                    IsFeatured = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Partners", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    TitleEn = table.Column<string>(type: "text", nullable: true),
                    Location = table.Column<string>(type: "text", nullable: false),
                    LocationEn = table.Column<string>(type: "text", nullable: true),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Progress = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    DescriptionEn = table.Column<string>(type: "text", nullable: true),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    Budget = table.Column<string>(type: "text", nullable: false),
                    FundsRaised = table.Column<string>(type: "text", nullable: false),
                    Beneficiaries = table.Column<string>(type: "text", nullable: false),
                    BeneficiariesEn = table.Column<string>(type: "text", nullable: true),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Partners = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PublicSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    Subject = table.Column<string>(type: "text", nullable: true),
                    City = table.Column<string>(type: "text", nullable: true),
                    Details = table.Column<string>(type: "text", nullable: false),
                    MetadataJson = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicSubmissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServiceContents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    TitleEn = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    DescriptionEn = table.Column<string>(type: "text", nullable: true),
                    Icon = table.Column<string>(type: "text", nullable: true),
                    Category = table.Column<string>(type: "text", nullable: true),
                    CategoryEn = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: true),
                    Details = table.Column<string>(type: "text", nullable: true),
                    DetailsEn = table.Column<string>(type: "text", nullable: true),
                    ExtendedInfo = table.Column<string>(type: "text", nullable: true),
                    ExtendedInfoEn = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceContents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SiteSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Statistics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false),
                    Label = table.Column<string>(type: "text", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Statistics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TeamMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Position = table.Column<string>(type: "text", nullable: false),
                    PositionEn = table.Column<string>(type: "text", nullable: true),
                    Region = table.Column<string>(type: "text", nullable: false),
                    RegionEn = table.Column<string>(type: "text", nullable: true),
                    Zone = table.Column<string>(type: "text", nullable: false),
                    ZoneEn = table.Column<string>(type: "text", nullable: true),
                    Photo = table.Column<string>(type: "text", nullable: true),
                    Bio = table.Column<string>(type: "text", nullable: true),
                    BioEn = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamMembers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EventAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventAttachments_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EventMedia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    MediaType = table.Column<string>(type: "text", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: true),
                    ContentType = table.Column<string>(type: "text", nullable: true),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    Caption = table.Column<string>(type: "text", nullable: true),
                    CaptionEn = table.Column<string>(type: "text", nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventMedia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventMedia_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConnectionRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequesterMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipientMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RespondedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConnectionRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConnectionRequests_Members_RecipientMemberId",
                        column: x => x.RecipientMemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConnectionRequests_Members_RequesterMemberId",
                        column: x => x.RequesterMemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MembershipApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    City = table.Column<string>(type: "text", nullable: true),
                    Province = table.Column<string>(type: "text", nullable: true),
                    Profession = table.Column<string>(type: "text", nullable: true),
                    Expertise = table.Column<string>(type: "text", nullable: true),
                    Motivation = table.Column<string>(type: "text", nullable: true),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MembershipApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MembershipApplications_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "MentorshipApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    ProfessionalSummary = table.Column<string>(type: "text", nullable: false),
                    Expertise = table.Column<string>(type: "text", nullable: false),
                    Objectives = table.Column<string>(type: "text", nullable: false),
                    Availability = table.Column<string>(type: "text", nullable: false),
                    PreferredLanguage = table.Column<string>(type: "text", nullable: false),
                    ConsentToShare = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CommitteeNotes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MentorshipApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MentorshipApplications_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NetworkingProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    Headline = table.Column<string>(type: "text", nullable: false),
                    Bio = table.Column<string>(type: "text", nullable: false),
                    Expertise = table.Column<string>(type: "text", nullable: false),
                    Sectors = table.Column<string>(type: "text", nullable: false),
                    City = table.Column<string>(type: "text", nullable: true),
                    Province = table.Column<string>(type: "text", nullable: true),
                    IsVisible = table.Column<bool>(type: "boolean", nullable: false),
                    AllowContactRequests = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NetworkingProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NetworkingProfiles_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PrivateConversations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberOneId = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberTwoId = table.Column<Guid>(type: "uuid", nullable: false),
                    RelationshipType = table.Column<string>(type: "text", nullable: false),
                    RelationshipId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastMessageAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrivateConversations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrivateConversations_Members_MemberOneId",
                        column: x => x.MemberOneId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrivateConversations_Members_MemberTwoId",
                        column: x => x.MemberTwoId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    FirstName = table.Column<string>(type: "text", nullable: true),
                    LastName = table.Column<string>(type: "text", nullable: true),
                    IsAdmin = table.Column<bool>(type: "boolean", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "NewsAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NewsId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewsAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NewsAttachments_News_NewsId",
                        column: x => x.NewsId,
                        principalTable: "News",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MentorshipMatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MentorApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    MenteeApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    MentorAccepted = table.Column<bool>(type: "boolean", nullable: false),
                    MenteeAccepted = table.Column<bool>(type: "boolean", nullable: false),
                    CommitteeNotes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActivatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MentorshipMatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MentorshipMatches_MentorshipApplications_MenteeApplicationId",
                        column: x => x.MenteeApplicationId,
                        principalTable: "MentorshipApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MentorshipMatches_MentorshipApplications_MentorApplicationId",
                        column: x => x.MentorApplicationId,
                        principalTable: "MentorshipApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConversationReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReporterMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    AdminNotes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConversationReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConversationReports_Members_ReporterMemberId",
                        column: x => x.ReporterMemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConversationReports_PrivateConversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "PrivateConversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PrivateMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrivateMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrivateMessages_Members_SenderMemberId",
                        column: x => x.SenderMemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrivateMessages_PrivateConversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "PrivateConversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PasswordResetTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "text", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordResetTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PasswordResetTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConnectionRequests_RecipientMemberId_Status_CreatedAt",
                table: "ConnectionRequests",
                columns: new[] { "RecipientMemberId", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ConnectionRequests_RequesterMemberId",
                table: "ConnectionRequests",
                column: "RequesterMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_Consultations_DisplayOrder",
                table: "Consultations",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_Consultations_IsActive",
                table: "Consultations",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationReports_ConversationId",
                table: "ConversationReports",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationReports_ReporterMemberId",
                table: "ConversationReports",
                column: "ReporterMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationReports_Status_CreatedAt",
                table: "ConversationReports",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_EventAttachments_EventId",
                table: "EventAttachments",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_EventMedia_EventId",
                table: "EventMedia",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_Date",
                table: "Events",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_Events_Status",
                table: "Events",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_GrantPrograms_DisplayOrder",
                table: "GrantPrograms",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_GrantPrograms_IsActive",
                table: "GrantPrograms",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Members_Email",
                table: "Members",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Members_Zone",
                table: "Members",
                column: "Zone");

            migrationBuilder.CreateIndex(
                name: "IX_MembershipApplications_CreatedAt",
                table: "MembershipApplications",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MembershipApplications_Email",
                table: "MembershipApplications",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_MembershipApplications_MemberId",
                table: "MembershipApplications",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_MembershipApplications_Status",
                table: "MembershipApplications",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_MentorshipApplications_MemberId_Role_Status",
                table: "MentorshipApplications",
                columns: new[] { "MemberId", "Role", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_MentorshipMatches_MenteeApplicationId",
                table: "MentorshipMatches",
                column: "MenteeApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_MentorshipMatches_MentorApplicationId",
                table: "MentorshipMatches",
                column: "MentorApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_MentorshipMatches_Status",
                table: "MentorshipMatches",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_NetworkingProfiles_IsVisible_AllowContactRequests",
                table: "NetworkingProfiles",
                columns: new[] { "IsVisible", "AllowContactRequests" });

            migrationBuilder.CreateIndex(
                name: "IX_NetworkingProfiles_MemberId",
                table: "NetworkingProfiles",
                column: "MemberId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_News_PublishedDate",
                table: "News",
                column: "PublishedDate");

            migrationBuilder.CreateIndex(
                name: "IX_News_Status",
                table: "News",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_NewsAttachments_NewsId",
                table: "NewsAttachments",
                column: "NewsId");

            migrationBuilder.CreateIndex(
                name: "IX_NewsletterCampaigns_CreatedAt",
                table: "NewsletterCampaigns",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_NewsletterSubscriptions_Email",
                table: "NewsletterSubscriptions",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NewsletterSubscriptions_IsActive",
                table: "NewsletterSubscriptions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_NewsletterSubscriptions_PreferredLanguage",
                table: "NewsletterSubscriptions",
                column: "PreferredLanguage");

            migrationBuilder.CreateIndex(
                name: "IX_NewsletterSubscriptions_UnsubscribeToken",
                table: "NewsletterSubscriptions",
                column: "UnsubscribeToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_CreatedAt",
                table: "Notifications",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsRead",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_Partners_DisplayOrder",
                table: "Partners",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_Partners_IsActive_IsFeatured",
                table: "Partners",
                columns: new[] { "IsActive", "IsFeatured" });

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_TokenHash",
                table: "PasswordResetTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_UserId",
                table: "PasswordResetTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PrivateConversations_LastMessageAt",
                table: "PrivateConversations",
                column: "LastMessageAt");

            migrationBuilder.CreateIndex(
                name: "IX_PrivateConversations_MemberOneId_MemberTwoId",
                table: "PrivateConversations",
                columns: new[] { "MemberOneId", "MemberTwoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrivateConversations_MemberTwoId",
                table: "PrivateConversations",
                column: "MemberTwoId");

            migrationBuilder.CreateIndex(
                name: "IX_PrivateMessages_ConversationId_CreatedAt",
                table: "PrivateMessages",
                columns: new[] { "ConversationId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PrivateMessages_SenderMemberId",
                table: "PrivateMessages",
                column: "SenderMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_Status",
                table: "Projects",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PublicSubmissions_Type_Status_CreatedAt",
                table: "PublicSubmissions",
                columns: new[] { "Type", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SiteSettings_Key",
                table: "SiteSettings",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Statistics_Key",
                table: "Statistics",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeamMembers_IsActive",
                table: "TeamMembers",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMembers_Order",
                table: "TeamMembers",
                column: "Order");

            migrationBuilder.CreateIndex(
                name: "IX_Users_MemberId",
                table: "Users",
                column: "MemberId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Associations");

            migrationBuilder.DropTable(
                name: "ConnectionRequests");

            migrationBuilder.DropTable(
                name: "Consultations");

            migrationBuilder.DropTable(
                name: "ConversationReports");

            migrationBuilder.DropTable(
                name: "Documents");

            migrationBuilder.DropTable(
                name: "EventAttachments");

            migrationBuilder.DropTable(
                name: "EventMedia");

            migrationBuilder.DropTable(
                name: "FooterLinks");

            migrationBuilder.DropTable(
                name: "GrantPrograms");

            migrationBuilder.DropTable(
                name: "MemberProfiles");

            migrationBuilder.DropTable(
                name: "MembershipApplications");

            migrationBuilder.DropTable(
                name: "MentorshipMatches");

            migrationBuilder.DropTable(
                name: "NavigationItems");

            migrationBuilder.DropTable(
                name: "NetworkingProfiles");

            migrationBuilder.DropTable(
                name: "NewsAttachments");

            migrationBuilder.DropTable(
                name: "NewsletterCampaigns");

            migrationBuilder.DropTable(
                name: "NewsletterSubscriptions");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "PageSections");

            migrationBuilder.DropTable(
                name: "Partners");

            migrationBuilder.DropTable(
                name: "PasswordResetTokens");

            migrationBuilder.DropTable(
                name: "PrivateMessages");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropTable(
                name: "PublicSubmissions");

            migrationBuilder.DropTable(
                name: "ServiceContents");

            migrationBuilder.DropTable(
                name: "SiteSettings");

            migrationBuilder.DropTable(
                name: "Statistics");

            migrationBuilder.DropTable(
                name: "TeamMembers");

            migrationBuilder.DropTable(
                name: "Events");

            migrationBuilder.DropTable(
                name: "MentorshipApplications");

            migrationBuilder.DropTable(
                name: "News");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "PrivateConversations");

            migrationBuilder.DropTable(
                name: "Members");
        }
    }
}
