using Microsoft.EntityFrameworkCore;
using TodoApi.Models;

namespace TodoApi.Data;


public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> optipns) : base(optipns) { }

    public DbSet<TodoItem> Todos => Set<TodoItem>();
}