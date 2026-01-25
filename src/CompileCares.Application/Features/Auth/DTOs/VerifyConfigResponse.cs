using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// File: CompileCares.Application/Features/Auth/DTOs/VerifyConfigResponse.cs
namespace CompileCares.Application.Features.Auth.DTOs
{
    public class VerifyConfigResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public VerifyConfigData Data { get; set; } = new VerifyConfigData();
    }

    public class VerifyConfigData
    {
        public int Users { get; set; }
        public int Doctors { get; set; }
        public int Medicines { get; set; }
        public int Doses { get; set; }
        public int Complaints { get; set; }
        public int AdvisedItems { get; set; }
        public int PrescriptionTemplates { get; set; }
        public bool CanQueryMedicines { get; set; }
        public bool CanQueryDoses { get; set; }
        public bool CanQueryUsers { get; set; }
    }
}
