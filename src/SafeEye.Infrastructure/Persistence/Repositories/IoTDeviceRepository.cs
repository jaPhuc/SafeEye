using SafeEye.Domain.Entities;
using SafeEye.Domain.Repositories;
using SafeEye.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace SafeEye.Infrastructure.Persistence.Repositories;

public sealed class IoTDeviceRepository(AppDbContext db) : IIoTDeviceRepository
{
    public Task<IoTDevice?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.IoTDevices.FirstOrDefaultAsync(d => d.Id == id, ct);

    public Task<IoTDevice?> GetByDeviceKeyAsync(string deviceKey, CancellationToken ct = default)
        => db.IoTDevices.FirstOrDefaultAsync(d => d.DeviceKey == deviceKey, ct);

    // ← NEW methods
    public Task<IoTDevice?> GetByFirebaseKeyAsync(string firebaseKey, CancellationToken ct = default)
        => db.IoTDevices.FirstOrDefaultAsync(d => d.FirebaseDeviceKey == firebaseKey, ct);

    public Task<List<IoTDevice>> GetAllWithFirebaseKeyAsync(CancellationToken ct = default)
        => db.IoTDevices
             .Where(d => d.FirebaseDeviceKey != null && d.FirebaseDeviceKey != "")
             .ToListAsync(ct);

    public Task<List<IoTDevice>> GetAllAsync(CancellationToken ct = default)
        => db.IoTDevices.OrderByDescending(d => d.CreatedAt).ToListAsync(ct);

    public async Task AddAsync(IoTDevice device, CancellationToken ct = default)
        => await db.IoTDevices.AddAsync(device, ct);
}