using CompileCares.Application.Features.Prescriptions.DTOs;

namespace CompileCares.Application.Features.Prescriptions.DTOs
{
    public class PrescriptionDetailDto : PrescriptionDto
    {
        public List<PrescriptionMedicineDto> Medicines { get; set; } = new();
        public List<PatientComplaintDto> Complaints { get; set; } = new();
        public List<PrescriptionAdvisedDto> AdvisedItems { get; set; } = new();

        // Doctor Signature Info
        public string? DoctorSignaturePath { get; set; }
        public string? DoctorDigitalSignature { get; set; }

        // Pharmacy Info
        public bool IsDispensed { get; set; }
        public DateTime? DispensedDate { get; set; }
        public string? DispensedBy { get; set; }
    }
}