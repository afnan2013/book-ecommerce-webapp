using Microsoft.EntityFrameworkCore;

namespace BookEcom.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Book> Books => Set<Book>();
}
