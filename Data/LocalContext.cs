using backendASPNET.Model;
using Microsoft.EntityFrameworkCore;


namespace backendASPNET.Data;

public class LocalContext : DbContext
{
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<BusStop> BusStops { get; set; } = null!;

    public LocalContext(DbContextOptions<LocalContext> options) : base(options)
    {
        
    }

}
