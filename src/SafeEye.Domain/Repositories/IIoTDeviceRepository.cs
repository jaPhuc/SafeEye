using SafeEye.Domain.Entities;

namespace SafeEye.Domain.Repositories;

public interface IIoTDeviceRepository
{
    Task<IoTDevice?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IoTDevice?> GetByDeviceKeyAsync(string deviceKey, CancellationToken ct = default);
    Task<IoTDevice?> GetByDeviceIdAsync(string deviceId, CancellationToken ct = default);
    Task<IoTDevice?> GetByFirebaseKeyAsync(string firebaseKey, CancellationToken ct = default);
    Task<List<IoTDevice>> GetAllWithFirebaseKeyAsync(CancellationToken ct = default);
    Task<List<IoTDevice>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(IoTDevice device, CancellationToken ct = default);
}