using System.Collections.Generic;

namespace COM_project3.Core.Services
{
    public class TrashDetection
    {
        public string label { get; set; }
        public float confidence { get; set; }
        public List<float> box { get; set; }
    }
}
