using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tattoo_Project.Data;

#nullable disable

namespace Tattoo_Project.Migrations
{
    [DbContext(typeof(TattooDbContext))]
    [Migration("20260709155000_AddUserProfileImageUrl")]
    partial class AddUserProfileImageUrl
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "10.0.9")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);
#pragma warning restore 612, 618
        }
    }
}
