using Bank.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace Bank.Web.Data;

public sealed class BankDbContext : DbContext
{
    public BankDbContext(DbContextOptions<BankDbContext> options) : base(options) { }

    public DbSet<KycUploadDetails> KycUploadDetails => Set<KycUploadDetails>();
    public DbSet<KycUploadImage> KycUploadImages => Set<KycUploadImage>();
    public DbSet<ZipFileUploadDetails> ZipFileUploadDetails => Set<ZipFileUploadDetails>();

    public DbSet<SearchResponseEncrypted> SearchResponseEncrypted => Set<SearchResponseEncrypted>();
    public DbSet<SearchResponseDecrypted> SearchResponseDecrypted => Set<SearchResponseDecrypted>();

    public DbSet<DownloadResponseEncrypted> DownloadResponseEncrypted => Set<DownloadResponseEncrypted>();
    public DbSet<DownloadResponseDecrypted> DownloadResponseDecrypted => Set<DownloadResponseDecrypted>();

    public DbSet<KycUpdationResponse> KycUpdationResponses => Set<KycUpdationResponse>();

    public DbSet<BankCustomerDetails> BankCustomerDetails => Set<BankCustomerDetails>();
    public DbSet<CustomerImage> CustomerImages => Set<CustomerImage>();

    public DbSet<BankEmployee> BankEmployees => Set<BankEmployee>();

    public DbSet<County> Counties => Set<County>();
    public DbSet<City> Cities => Set<City>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Table names (match your Excel)
        modelBuilder.Entity<KycUploadDetails>().ToTable("KYCUpload_Details");
        modelBuilder.Entity<KycUploadImage>().ToTable("KYCUpload_Images");
        modelBuilder.Entity<ZipFileUploadDetails>().ToTable("ZipFileUploadDetails");
        modelBuilder.Entity<SearchResponseEncrypted>().ToTable("SearchResponseEncrypted");
        modelBuilder.Entity<SearchResponseDecrypted>().ToTable("SearchResponseDecrypted");
        modelBuilder.Entity<DownloadResponseEncrypted>().ToTable("DownloadResponseEncrypted");
        modelBuilder.Entity<DownloadResponseDecrypted>().ToTable("DownloadResponseDecrypted");
        modelBuilder.Entity<KycUpdationResponse>().ToTable("KYCUpdationResponse");
        modelBuilder.Entity<BankCustomerDetails>().ToTable("BankCustomerDetails");
        modelBuilder.Entity<CustomerImage>().ToTable("CustomerImages");
        modelBuilder.Entity<County>().ToTable("County");
        modelBuilder.Entity<City>().ToTable("City");
        modelBuilder.Entity<BankEmployee>().ToTable("BankEmployee");

        // Relationships
        modelBuilder.Entity<KycUploadImage>()
            .HasOne(x => x.KycUpload)
            .WithMany(u => u.Images)
            .HasForeignKey(x => x.KycUploadId);

        modelBuilder.Entity<ZipFileUploadDetails>()
            .HasOne(z => z.KycUpload)
            .WithMany()
            .HasForeignKey(z => z.KycUploadId);

        modelBuilder.Entity<SearchResponseEncrypted>()
            .HasOne(x => x.KycUpload).WithMany()
            .HasForeignKey(x => x.KycUploadId);

        modelBuilder.Entity<SearchResponseDecrypted>()
            .HasOne(x => x.KycUpload).WithMany()
            .HasForeignKey(x => x.KycUploadId);

        modelBuilder.Entity<DownloadResponseEncrypted>()
            .HasOne(x => x.KycUpload).WithMany()
            .HasForeignKey(x => x.KycUploadId);

        modelBuilder.Entity<DownloadResponseDecrypted>()
            .HasOne(x => x.KycUpload).WithMany()
            .HasForeignKey(x => x.KycUploadId);

        modelBuilder.Entity<KycUpdationResponse>()
            .HasOne(x => x.KycUpload).WithMany()
            .HasForeignKey(x => x.KycUploadId);

        modelBuilder.Entity<CustomerImage>()
            .HasOne(x => x.BankCustomer)
            .WithMany(c => c.CustomerImages)
            .HasForeignKey(x => x.BankCustomerId);

        // Master data relationships
        modelBuilder.Entity<City>()
            .HasOne(x => x.County)
            .WithMany(c => c.Cities)
            .HasForeignKey(x => x.CountyId);

        modelBuilder.Entity<KycUploadDetails>()
            .HasOne(x => x.County).WithMany()
            .HasForeignKey(x => x.CountyId);

        modelBuilder.Entity<KycUploadDetails>()
            .HasOne(x => x.City).WithMany()
            .HasForeignKey(x => x.CityId);

        modelBuilder.Entity<BankCustomerDetails>()
            .HasOne(x => x.County).WithMany()
            .HasForeignKey(x => x.CountyId);

        modelBuilder.Entity<BankCustomerDetails>()
            .HasOne(x => x.City).WithMany()
            .HasForeignKey(x => x.CityId);

        modelBuilder.Entity<BankCustomerDetails>()
            .HasOne(x => x.KycUpload).WithMany()
            .HasForeignKey(x => x.KycUploadId);
        modelBuilder.Entity<BankEmployee>()
            .HasIndex(x => x.EmployeeCode)
            .IsUnique();

        modelBuilder.Entity<BankEmployee>()
            .HasIndex(x => x.Email)
            .IsUnique();

        // Indexes / uniques
        modelBuilder.Entity<KycUploadDetails>().HasIndex(x => x.RequestRef).IsUnique();
        modelBuilder.Entity<KycUploadDetails>().HasIndex(x => x.IdentityHash);

        modelBuilder.Entity<ZipFileUploadDetails>().HasIndex(x => x.KycUploadId).IsUnique();
        modelBuilder.Entity<BankCustomerDetails>().HasIndex(x => x.KycUploadId).IsUnique();

        modelBuilder.Entity<KycUploadImage>().HasIndex(x => x.FileHashSha256);
        modelBuilder.Entity<CustomerImage>().HasIndex(x => x.FileHashSha256);

        modelBuilder.Entity<County>().HasIndex(x => x.CountyName).IsUnique();
        modelBuilder.Entity<City>().HasIndex(x => new { x.CountyId, x.CityName }).IsUnique();

        modelBuilder.Entity<DownloadResponseDecrypted>()
            .Property(x => x.PayloadJson)
            .HasColumnType("jsonb");
    }
}