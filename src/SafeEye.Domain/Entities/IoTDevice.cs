namespace SafeEye.Domain.Entities;

public sealed class IoTDevice
{
    private IoTDevice() { }

    public Guid Id { get; private set; }
    public string DeviceKey { get; private set; } = string.Empty;
    public string Label { get; private set; } = string.Empty;
    /// <summary>Node key in Firebase Realtime Database (e.g. "device1").</summary>
    public string? FirebaseDeviceKey { get; private set; }   // ← NEW
    public DateTime? LastSeen { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public ICollection<GuardianDevice> GuardianDevices { get; private set; } = new List<GuardianDevice>();
    public ICollection<SosEvent> SosEvents { get; private set; } = new List<SosEvent>();
    // ICollection<LocationUpdate> removed

    public static IoTDevice Create(string deviceKey, string label, string? firebaseDeviceKey = null) => new()
    {
        Id = Guid.NewGuid(),
        DeviceKey = deviceKey,
        Label = label,
        FirebaseDeviceKey = firebaseDeviceKey,
        CreatedAt = DateTime.UtcNow,
    };

    public void UpdateLastSeen() => LastSeen = DateTime.UtcNow;
    public void UpdateLabel(string label) => Label = label;
    public void SetFirebaseKey(string firebaseDeviceKey) => FirebaseDeviceKey = firebaseDeviceKey;
}