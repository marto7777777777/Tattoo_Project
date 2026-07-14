using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Tattoo_Project.Data;
#nullable disable
namespace Tattoo_Project.Migrations;
[DbContext(typeof(TattooDbContext))]
[Migration("20260714090000_AddAiTattooStudio")]
public partial class AddAiTattooStudio : Migration
{
 protected override void Up(MigrationBuilder migrationBuilder)
 {
  migrationBuilder.CreateTable(name:"AiTattooProjects",columns:table=>new{Id=table.Column<int>(nullable:false).Annotation("SqlServer:Identity","1, 1"),UserId=table.Column<string>(nullable:false),Title=table.Column<string>(maxLength:140,nullable:false),TattooStyle=table.Column<string>(maxLength:100,nullable:false),Placement=table.Column<string>(maxLength:100,nullable:false),InitialDescription=table.Column<string>(maxLength:3000,nullable:false),InitialReferenceImageUrl=table.Column<string>(nullable:true),IsFreeProject=table.Column<bool>(nullable:false),FreeEditsUsed=table.Column<int>(nullable:false),EditingAccessUntil=table.Column<DateTime>(nullable:true),CreatedAt=table.Column<DateTime>(nullable:false),UpdatedAt=table.Column<DateTime>(nullable:false)},constraints:table=>{table.PrimaryKey("PK_AiTattooProjects",x=>x.Id);table.ForeignKey("FK_AiTattooProjects_AspNetUsers_UserId",x=>x.UserId,"AspNetUsers","Id",onDelete:ReferentialAction.Cascade);});
  migrationBuilder.CreateTable(name:"AiProjectPayments",columns:table=>new{Id=table.Column<int>(nullable:false).Annotation("SqlServer:Identity","1, 1"),UserId=table.Column<string>(nullable:false),AiTattooProjectId=table.Column<int>(nullable:false),StripeCheckoutSessionId=table.Column<string>(maxLength:255,nullable:false),StripePaymentIntentId=table.Column<string>(nullable:true),AmountInMinorUnits=table.Column<long>(nullable:false),Currency=table.Column<string>(maxLength:3,nullable:false),Status=table.Column<string>(maxLength:30,nullable:false),CreatedAt=table.Column<DateTime>(nullable:false),PaidAt=table.Column<DateTime>(nullable:true),AccessGrantedUntil=table.Column<DateTime>(nullable:true)},constraints:table=>{table.PrimaryKey("PK_AiProjectPayments",x=>x.Id);table.ForeignKey("FK_AiProjectPayments_AiTattooProjects_AiTattooProjectId",x=>x.AiTattooProjectId,"AiTattooProjects","Id",onDelete:ReferentialAction.Cascade);});
  migrationBuilder.CreateTable(name:"AiTattooVersions",columns:table=>new{Id=table.Column<int>(nullable:false).Annotation("SqlServer:Identity","1, 1"),AiTattooProjectId=table.Column<int>(nullable:false),VersionNumber=table.Column<int>(nullable:false),Prompt=table.Column<string>(maxLength:3000,nullable:false),ImageUrl=table.Column<string>(maxLength:500,nullable:false),ParentVersionId=table.Column<int>(nullable:true),CreatedAt=table.Column<DateTime>(nullable:false)},constraints:table=>{table.PrimaryKey("PK_AiTattooVersions",x=>x.Id);table.ForeignKey("FK_AiTattooVersions_AiTattooProjects_AiTattooProjectId",x=>x.AiTattooProjectId,"AiTattooProjects","Id",onDelete:ReferentialAction.Cascade);table.ForeignKey("FK_AiTattooVersions_AiTattooVersions_ParentVersionId",x=>x.ParentVersionId,"AiTattooVersions","Id",onDelete:ReferentialAction.NoAction);});
  migrationBuilder.CreateIndex("IX_AiTattooProjects_UserId_IsFreeProject","AiTattooProjects",new[]{"UserId","IsFreeProject"},unique:true,filter:"[IsFreeProject] = 1");
  migrationBuilder.CreateIndex("IX_AiProjectPayments_AiTattooProjectId","AiProjectPayments","AiTattooProjectId"); migrationBuilder.CreateIndex("IX_AiProjectPayments_StripeCheckoutSessionId","AiProjectPayments","StripeCheckoutSessionId",unique:true);
  migrationBuilder.CreateIndex("IX_AiTattooVersions_AiTattooProjectId_VersionNumber","AiTattooVersions",new[]{"AiTattooProjectId","VersionNumber"},unique:true); migrationBuilder.CreateIndex("IX_AiTattooVersions_ParentVersionId","AiTattooVersions","ParentVersionId");
 }
 protected override void Down(MigrationBuilder migrationBuilder){migrationBuilder.DropTable("AiProjectPayments");migrationBuilder.DropTable("AiTattooVersions");migrationBuilder.DropTable("AiTattooProjects");}
}
