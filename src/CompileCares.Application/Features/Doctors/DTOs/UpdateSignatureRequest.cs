using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileCares.Application.Features.Doctors.DTOs
{
    public class UpdateSignatureRequest
    {
        public string SignaturePath { get; set; } = string.Empty;
        public string? DigitalSignature { get; set; }
    }
}
