namespace CompileCares.Application.Features.Master.DTOs
{
    public class CreateDoseRequest
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int SortOrder { get; set; } = 0;
    }
}