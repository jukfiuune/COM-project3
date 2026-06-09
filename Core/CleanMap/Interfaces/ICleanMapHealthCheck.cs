namespace Core.CleanMap;

public interface ICleanMapHealthCheck
{
    Task<bool> CanConnectAsync(CancellationToken cancellationToken);
}
