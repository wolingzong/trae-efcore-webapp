using EfCoreWebApp.Models;
using Microsoft.EntityFrameworkCore;

namespace EfCoreWebApp.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<Todo> Todos => Set<Todo>();
    public DbSet<Product> Products => Set<Product>();
}
