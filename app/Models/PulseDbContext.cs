using Microsoft.EntityFrameworkCore;

namespace app.Models;

public partial class PulseDbContext : DbContext
{
    public PulseDbContext()
    {
    }

    public PulseDbContext(DbContextOptions<PulseDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<CheckResult> CheckResults { get; set; }

    public virtual DbSet<HttpCode> HttpCodes { get; set; }

    public virtual DbSet<HttpMethod> HttpMethods { get; set; }

    public virtual DbSet<Monitor> Monitors { get; set; }

    public virtual DbSet<Service> Services { get; set; }

    public virtual DbSet<ServiceStatus> ServiceStatuses { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseNpgsql("Name=ConnectionStrings:ConnStr");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CheckResult>(entity =>
        {
            entity.HasKey(e => e.CheckResultId).HasName("check_results_pkey");

            entity.ToTable("check_results");

            entity.Property(e => e.CheckResultId).HasColumnName("check_result_id");
            entity.Property(e => e.CheckedAt).HasColumnName("checked_at");
            entity.Property(e => e.DurationMs).HasColumnName("duration_ms");
            entity.Property(e => e.HttpCodeId).HasColumnName("http_code_id");
            entity.Property(e => e.MonitorId).HasColumnName("monitor_id");
            entity.Property(e => e.Outcome)
                .HasMaxLength(30)
                .HasColumnName("outcome");

            entity.HasOne(d => d.HttpCode).WithMany(p => p.CheckResults)
                .HasForeignKey(d => d.HttpCodeId)
                .HasConstraintName("check_results_http_code_id_fkey");

            entity.HasOne(d => d.Monitor).WithMany(p => p.CheckResults)
                .HasForeignKey(d => d.MonitorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("check_results_monitor_id_fkey");
        });

        modelBuilder.Entity<HttpCode>(entity =>
        {
            entity.HasKey(e => e.HttpCodeId).HasName("http_codes_pkey");

            entity.ToTable("http_codes");

            entity.HasIndex(e => e.Code, "http_codes_http_code_key").IsUnique();

            entity.Property(e => e.HttpCodeId).HasColumnName("http_code_id");
            entity.Property(e => e.Code).HasColumnName("http_code");
        });

        modelBuilder.Entity<HttpMethod>(entity =>
        {
            entity.HasKey(e => e.HttpMethodId).HasName("http_methods_pkey");

            entity.ToTable("http_methods");

            entity.HasIndex(e => e.Method, "http_methods_http_method_key").IsUnique();

            entity.Property(e => e.HttpMethodId).HasColumnName("http_method_id");
            entity.Property(e => e.Method)
                .HasColumnType("character varying")
                .HasMaxLength(10)
                .HasColumnName("http_method");
        });

        modelBuilder.Entity<Monitor>(entity =>
        {
            entity.HasKey(e => e.MonitorId).HasName("monitors_pkey");

            entity.ToTable("monitors");

            entity.Property(e => e.MonitorId).HasColumnName("monitor_id");
            entity.Property(e => e.HttpMethodId).HasColumnName("http_method_id");
            entity.Property(e => e.IntervalSeconds).HasColumnName("interval_seconds");
            entity.Property(e => e.Path)
                .HasColumnType("character varying")
                .HasMaxLength(50)
                .HasColumnName("path");
            entity.Property(e => e.ServiceId).HasColumnName("service_id");

            entity.HasOne(d => d.HttpMethod).WithMany(p => p.Monitors)
                .HasForeignKey(d => d.HttpMethodId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("monitors_http_method_id_fkey");

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
                .HasMaxLength(50)
                .HasColumnName("base_url");
            entity.Property(e => e.ServiceStatusId).HasColumnName("service_status_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.ServiceStatus).WithMany(p => p.Services)
                .HasForeignKey(d => d.ServiceStatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("services_service_status_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.Services)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("services_user_id_fkey");
        });

        modelBuilder.Entity<ServiceStatus>(entity =>
        {
            entity.HasKey(e => e.ServiceStatusId).HasName("service_statuses_pkey");

            entity.ToTable("service_statuses");

            entity.HasIndex(e => e.Status, "service_statuses_service_status_key").IsUnique();

            entity.Property(e => e.ServiceStatusId).HasColumnName("service_status_id");
            entity.Property(e => e.Status)
                .HasColumnType("character varying")
                .HasMaxLength(10)
                .HasColumnName("service_status");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("users_pkey");

            entity.ToTable("users");

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Email)
                .HasMaxLength(50)
                .HasColumnName("email");
            entity.Property(e => e.PasswordHash)
                .HasColumnType("character varying")
                .HasColumnName("password_hash");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
