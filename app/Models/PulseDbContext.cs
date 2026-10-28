using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace app.Models;

public class PulseDbContext : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>
{
    public PulseDbContext()
    {
    }

    public PulseDbContext(DbContextOptions<PulseDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<CheckResult> CheckResults { get; set; }

    public virtual DbSet<Monitor> Monitors { get; set; }

    public virtual DbSet<Service> Services { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<CheckResult>(entity =>
        {
            entity.HasKey(e => e.CheckResultId).HasName("check_results_pkey");

            entity.ToTable("check_results");

            entity.Property(e => e.CheckResultId).HasColumnName("check_result_id");
            entity.Property(e => e.CheckedAt).HasColumnName("checked_at");
            entity.Property(e => e.DurationMs).HasColumnName("duration_ms");
            entity.Property(e => e.HttpStatusCode).HasColumnName("http_status_code")
                .HasConversion<int>();
            entity.Property(e => e.MonitorId).HasColumnName("monitor_id");
            entity.Property(e => e.Outcome)
                .HasMaxLength(30)
                .HasColumnName("outcome");

            entity.HasOne(d => d.Monitor).WithMany(p => p.CheckResults)
                .HasForeignKey(d => d.MonitorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("check_results_monitor_id_fkey");
        });

        modelBuilder.Entity<Monitor>(entity =>
        {
            entity.HasKey(e => e.MonitorId).HasName("monitors_pkey");

            entity.ToTable("monitors");

            entity.Property(e => e.MonitorId).HasColumnName("monitor_id");
            entity.Property(e => e.HttpMethod)
                .HasColumnName("http_method")
                .HasConversion(
                    method => method.Method,
                    value => new HttpMethod(value));
            entity.Property(e => e.IntervalSeconds).HasColumnName("interval_seconds");
            entity.Property(e => e.Path)
                .HasColumnType("character varying")
                .HasColumnName("path");
            entity.Property(e => e.ServiceId).HasColumnName("service_id");

            entity.HasOne(d => d.Service).WithMany(p => p.Monitors)
                .HasForeignKey(d => d.ServiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("monitors_service_id_fkey");
        });

        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasKey(e => e.ServiceId).HasName("services_pkey");

            entity.ToTable("services");

            entity.Property(e => e.ServiceId).HasColumnName("service_id");
            entity.Property(e => e.BaseUrl)
                .HasColumnType("character varying")
                .HasColumnName("base_url");
            entity.Property(e => e.Name).HasColumnName("name")
                .HasColumnType("character varying")
                .HasMaxLength(30);
            entity.Property(e => e.ServiceStatus).HasColumnName("service_status")
                .HasConversion<string>();
            entity.Property(e => e.ApplicationUserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Services)
                .HasForeignKey(d => d.ApplicationUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("services_user_id_fkey");
        });
    }
}