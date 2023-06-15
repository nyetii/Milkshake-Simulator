using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Milkshake.Models;

namespace Kimi.Services.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
        }

        public DbSet<Servers>? Servers { get; set; }
        public DbSet<MilkshakeInstance>? Milkshake { get; set; }
        public DbSet<Source>? Source { get; set; }
        public DbSet<Template>? Template { get; set; }
        public DbSet<Topping>? TemplateProperties { get; set; }
    }
}
