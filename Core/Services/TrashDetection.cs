using System.Collections.Generic;

namespace Core.Services
{
    public class TrashDetection
    {
        public string label { get; set; } = string.Empty;
        public float confidence { get; set; }
        public List<float> box { get; set; } = null!;
        public string? annotatedImage { get; set; }
    }
}
