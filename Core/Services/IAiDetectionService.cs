using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Core.Services
{
    public interface IAiDetectionService
    {
        Task<List<TrashDetection>> DetectTrashAsync(Stream imageStream, string fileName);
    }
}
