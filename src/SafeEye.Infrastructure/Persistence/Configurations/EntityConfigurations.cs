// LocationUpdateConfiguration removed
// IoTDeviceConfiguration adds firebase_device_key
using SafeEye.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SafeEye.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("users"); b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(80).IsRequired();
        b.Property(x => x.Email).HasColumnName("email").HasMaxLength(256).IsRequired();
        b.Property(x => x.PhoneNumber).HasColumnName("phone_number").HasMaxLength(20);
        b.Property(x => x.PasswordHash).HasColumnName("password_hash");
        b.Property(x => x.GoogleId).HasColumnName("google_id").HasMaxLength(128);
        b.Property(x => x.FcmToken).HasColumnName("fcm_token");
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        b.HasIndex(x => x.Email).IsUnique();
        b.HasIndex(x => x.GoogleId).IsUnique();
    }
}

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        b.ToTable("refresh_tokens"); b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.UserId).HasColumnName("user_id");
        b.Property(x => x.Token).HasColumnName("token").IsRequired();
        b.Property(x => x.ExpiresAt).HasColumnName("expires_at");
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.HasIndex(x => x.Token).IsUnique();
        b.HasOne(x => x.User).WithMany(u => u.RefreshTokens)
         .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class IoTDeviceConfiguration : IEntityTypeConfiguration<IoTDevice>
{
    public void Configure(EntityTypeBuilder<IoTDevice> b)
    {
        b.ToTable("iot_devices"); b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.DeviceKey).HasColumnName("device_key").HasMaxLength(64).IsRequired();
        b.Property(x => x.Label).HasColumnName("label").HasMaxLength(80).IsRequired();
        b.Property(x => x.FirebaseDeviceKey).HasColumnName("firebase_device_key").HasMaxLength(100);
        b.Property(x => x.FirebaseUserId).HasColumnName("firebase_user_id").HasMaxLength(100);
        b.Property(x => x.BatteryPercent).HasColumnName("battery_percent");
        b.Property(x => x.UptimeSeconds).HasColumnName("uptime_seconds");
        b.Property(x => x.LastSeen).HasColumnName("last_seen");
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.HasIndex(x => x.DeviceKey).IsUnique();
        b.HasIndex(x => x.FirebaseDeviceKey);
        b.HasIndex(x => x.FirebaseUserId);
    }
}

public class GuardianDeviceConfiguration : IEntityTypeConfiguration<GuardianDevice>
{
    public void Configure(EntityTypeBuilder<GuardianDevice> b)
    {
        b.ToTable("guardian_devices"); b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.GuardianId).HasColumnName("guardian_id");
        b.Property(x => x.DeviceId).HasColumnName("device_id");
        b.Property(x => x.Label).HasColumnName("label").HasMaxLength(80).IsRequired();
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        b.HasIndex(x => new { x.GuardianId, x.DeviceId }).IsUnique();
        b.HasOne(x => x.Guardian).WithMany(u => u.WatchedDevices)
         .HasForeignKey(x => x.GuardianId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Device).WithMany(d => d.GuardianDevices)
         .HasForeignKey(x => x.DeviceId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class SosEventConfiguration : IEntityTypeConfiguration<SosEvent>
{
    public void Configure(EntityTypeBuilder<SosEvent> b)
    {
        b.ToTable("sos_events"); b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.DeviceId).HasColumnName("device_id");
        b.Property(x => x.Latitude).HasColumnName("latitude");
        b.Property(x => x.Longitude).HasColumnName("longitude");
        b.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.ResolvedAt).HasColumnName("resolved_at");
        b.Property(x => x.ResolvedById).HasColumnName("resolved_by_id");
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.HasIndex(x => x.DeviceId); b.HasIndex(x => x.Status);
        b.HasOne(x => x.Device).WithMany(d => d.SosEvents)
         .HasForeignKey(x => x.DeviceId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.ResolvedBy).WithMany()
         .HasForeignKey(x => x.ResolvedById).OnDelete(DeleteBehavior.SetNull);
    }
}