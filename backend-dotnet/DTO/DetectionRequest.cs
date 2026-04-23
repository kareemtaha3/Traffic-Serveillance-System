using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace backend_dotnet.dto
{
    public class DetectionRequest
    {
        public string LicensePlate { get; set; } = string.Empty;
        public int Speed { get; set; }
        public string CameraId { get; set; } = string.Empty;
    }
}