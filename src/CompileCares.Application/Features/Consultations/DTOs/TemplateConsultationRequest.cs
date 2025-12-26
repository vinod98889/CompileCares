using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileCares.Application.Features.Consultations.DTOs
{
    public class TemplateConsultationRequest
    {
        public Guid PatientId { get; set; }
        public Guid DoctorId { get; set; }
        public Guid TemplateId { get; set; }
    }
}
