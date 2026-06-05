namespace Core.CleanMap.Interfaces;

public interface IAiDetectionService
{
    Task<IReadOnlyList<TrashDetection>> DetectTrashAsync(string base64Image, CancellationToken cancellationToken = default);
}
