namespace CompileCares.Application.Features.Templates.DTOs
{
    public class TemplateDetailDto : PrescriptionTemplateDto
    {
        public List<TemplateComplaintDto> Complaints { get; set; } = new();
        public List<TemplateMedicineDto> Medicines { get; set; } = new();
        public List<TemplateAdvisedDto> AdvisedItems { get; set; } = new();
    }
}