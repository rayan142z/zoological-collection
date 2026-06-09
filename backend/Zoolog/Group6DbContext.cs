using Microsoft.EntityFrameworkCore;
using Zoolog.Models;

namespace Zoolog; 

public class Group6DbContext : DbContext
{
    public Group6DbContext(DbContextOptions<Group6DbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Taxonomy> Taxonomies { get; set; }
    public DbSet<Location> Locations { get; set; }
    public DbSet<Collection> Collections { get; set; }
    public DbSet<Specimen> Specimens { get; set; }
    public DbSet<Loan> Loans { get; set; }
}