using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace COM_project3.Core.Services
{
    public interface IAiDetectionService
    {
        Task<List<TrashDetection>> DetectTrashAsync(Stream imageStream, string fileName);
    }
}
