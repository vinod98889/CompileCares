namespace CompileCares.Application.Features.Doctors.DTOs
{
    public class DoctorSearchRequest
    {
        public string? SearchTerm { get; set; }
        public string? RegistrationNumber { get; set; }
        public string? Specialization { get; set; }
        public string? Department { get; set; }
        public bool? IsAvailable { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsVerified { get; set; }
        public decimal? MinConsultationFee { get; set; }
        public decimal? MaxConsultationFee { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SortBy { get; set; } = "Name";
        public bool SortDescending { get; set; } = false;
    }
}