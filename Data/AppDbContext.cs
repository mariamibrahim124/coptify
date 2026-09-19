using Microsoft.EntityFrameworkCore;
using Coptify.Web.Models;

namespace Coptify.Web.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<Video> Videos => Set<Video>();
    public DbSet<SiteContent> SiteContents => Set<SiteContent>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
}
