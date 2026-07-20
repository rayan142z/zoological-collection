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

    public DbSet<CollectionFavorite> CollectionFavorites { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");

            entity.Property(u => u.Id).HasColumnName("id");
            entity.Property(u => u.Username).HasColumnName("username").HasMaxLength(100).IsRequired();
            entity.Property(u => u.Email).HasColumnName("email").HasMaxLength(255).IsRequired();
            entity.Property(u => u.Pass).HasColumnName("pass").IsRequired();
            entity.Property(u => u.UserRole).HasColumnName("user_role").HasMaxLength(30).HasDefaultValue("user").IsRequired();
            entity.Property(u => u.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("active").IsRequired();
            entity.Property(u => u.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("getdate()");
            entity.Property(u => u.Description).HasColumnName("description");
            entity.Property(u => u.Job).HasColumnName("job");
        });

        modelBuilder.Entity<Taxonomy>(entity =>
        {
            entity.ToTable("taxonomy");

            entity.Property(t => t.Id).HasColumnName("id");
            entity.Property(t => t.Kingdom).HasColumnName("kingdom").HasMaxLength(100);
            entity.Property(t => t.Phylum).HasColumnName("phylum").HasMaxLength(100);
            entity.Property(t => t.Class).HasColumnName("class").HasMaxLength(100);
            entity.Property(t => t.Orders).HasColumnName("orders").HasMaxLength(100);
            entity.Property(t => t.Family).HasColumnName("family").HasMaxLength(100);
            entity.Property(t => t.Genus).HasColumnName("genus").HasMaxLength(100);
            entity.Property(t => t.Species).HasColumnName("species").HasMaxLength(100).IsRequired();
            entity.Property(t => t.Validated).HasColumnName("validated").HasDefaultValue(false);
        });

        modelBuilder.Entity<Location>(entity =>
        {
            entity.ToTable("location");

            entity.Property(l => l.Id).HasColumnName("id");
            entity.Property(l => l.Name).HasColumnName("name").HasMaxLength(255);
            entity.Property(l => l.Latitude).HasColumnName("latitude");
            entity.Property(l => l.Longitude).HasColumnName("longitude");
            entity.Property(l => l.Country).HasColumnName("country").HasMaxLength(100);
            entity.Property(l => l.Region).HasColumnName("region").HasMaxLength(100);
        });

        modelBuilder.Entity<Collection>(entity =>
        {
            entity.ToTable("collection");

            entity.Property(c => c.Id).HasColumnName("id");
            entity.Property(c => c.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            entity.Property(c => c.Description).HasColumnName("description");
            entity.Property(c => c.IsPublic).HasColumnName("is_public").HasDefaultValue(false);
            entity.Property(c => c.CreatedBy).HasColumnName("created_by");
            entity.Property(c => c.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("getdate()");

            entity.HasOne(c => c.Creator)
                .WithMany()
                .HasForeignKey(c => c.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Specimen>(entity =>
        {
            entity.ToTable("specimen");

            entity.Property(s => s.Id).HasColumnName("id");
            entity.Property(s => s.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            entity.Property(s => s.Description).HasColumnName("description");
            entity.Property(s => s.DateCollected).HasColumnName("date_collected");
            entity.Property(s => s.Status).HasColumnName("status").HasMaxLength(30);
            entity.Property(s => s.Size).HasColumnName("size").HasMaxLength(100);
            entity.Property(s => s.Weight).HasColumnName("weight");
            entity.Property(s => s.BirthYear).HasColumnName("birth_year");
            entity.Property(s => s.PhotoPath).HasColumnName("photo_path").HasMaxLength(500);
            entity.Property(s => s.LocationId).HasColumnName("location_id");
            entity.Property(s => s.TaxonomyId).HasColumnName("taxonomy_id");
            entity.Property(s => s.CollectionId).HasColumnName("collection_id");
            entity.Property(s => s.AddedBy).HasColumnName("added_by");
            entity.Property(s => s.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("getdate()");

            entity.HasOne(s => s.Location)
                .WithMany()
                .HasForeignKey(s => s.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(s => s.Taxonomy)
                .WithMany()
                .HasForeignKey(s => s.TaxonomyId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(s => s.Collection)
                .WithMany()
                .HasForeignKey(s => s.CollectionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(s => s.AddedByUser)
                .WithMany()
                .HasForeignKey(s => s.AddedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Loan>(entity =>
        {
            entity.ToTable("loan");

            entity.Property(l => l.Id).HasColumnName("id");
            entity.Property(l => l.SpecimenId).HasColumnName("specimen_id");
            entity.Property(l => l.LoanedTo).HasColumnName("loaned_to").HasMaxLength(255).IsRequired();
            entity.Property(l => l.LoanDate).HasColumnName("loan_date");
            entity.Property(l => l.ReturnDate).HasColumnName("return_date");
            entity.Property(l => l.Status).HasColumnName("status").HasMaxLength(30);
            entity.Property(l => l.Notes).HasColumnName("notes");

            entity.HasOne(l => l.Specimen)
                .WithMany()
                .HasForeignKey(l => l.SpecimenId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CollectionFavorite>(entity =>
        {
            entity.ToTable("collection_favorites");

            // Zusammengesetzter Primärschlüssel (Composite Key)
            entity.HasKey(cf => new { cf.UserId, cf.CollectionId });

            entity.Property(cf => cf.UserId).HasColumnName("user_id");
            entity.Property(cf => cf.CollectionId).HasColumnName("collection_id");
            
            entity.Property(cf => cf.FavoritedAt)
                .HasColumnName("favorited_at")
                .HasDefaultValueSql("GETDATE()");

            entity.HasOne<Collection>() 
                .WithMany()
                .HasForeignKey(cf => cf.CollectionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
