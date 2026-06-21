using SafeEye.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SafeEye.Infrastructure.Persistence.Configurations;

public class IoTDeviceConfiguration : IEntityTypeConfiguration<IoTDevice>
{
    public void Configure(EntityTypeBuilder<IoTDevice> b)
    {
        b.ToTable("iot_devices"); b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.DeviceKey).HasColumnName("device_key").HasMaxLength(64).IsRequired();
        b.Property(x => x.DeviceId).HasColumnName("device_id").HasMaxLength(100).IsRequired();
        b.Property(x => x.SecretKeyHash).HasColumnName("secret_key_hash").IsRequired();
        b.Property(x => x.Label).HasColumnName("label").HasMaxLength(80).IsRequired();
        b.Property(x => x.FirebaseDeviceKey).HasColumnName("firebase_device_key").HasMaxLength(100);
        b.Property(x => x.BatteryPercent).HasColumnName("battery_percent");
        b.Property(x => x.UptimeSeconds).HasColumnName("uptime_seconds");
        b.Property(x => x.LastSeen).HasColumnName("last_seen");
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.HasIndex(x => x.DeviceKey).IsUnique();
        b.HasIndex(x => x.DeviceId).IsUnique();
        b.HasIndex(x => x.FirebaseDeviceKey);
    }
}

public class GuardianDeviceConfiguration : IEntityTypeConfiguration<GuardianDevice>
{
    public void Configure(EntityTypeBuilder<GuardianDevice> b)
    {
        b.ToTable("guardian_devices"); b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.GuardianUuid).HasColumnName("guardian_uuid").HasMaxLength(100).IsRequired();
        b.Property(x => x.FcmToken).HasColumnName("fcm_token").IsRequired();
        b.Property(x => x.DeviceId).HasColumnName("device_id");
        b.Property(x => x.Label).HasColumnName("label").HasMaxLength(80).IsRequired();
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        b.HasIndex(x => x.GuardianUuid);
        b.HasIndex(x => new { x.GuardianUuid, x.DeviceId }).IsUnique();
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
        b.Property(x => x.ResolvedByGuardianUuid).HasColumnName("resolved_by_guardian_uuid").HasMaxLength(100);
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.HasIndex(x => x.DeviceId); b.HasIndex(x => x.Status);
        b.HasOne(x => x.Device).WithMany(d => d.SosEvents)
         .HasForeignKey(x => x.DeviceId).OnDelete(DeleteBehavior.Cascade);
    }
}