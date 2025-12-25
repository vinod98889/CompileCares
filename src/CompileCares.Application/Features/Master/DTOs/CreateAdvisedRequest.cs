namespace CompileCares.Application.Features.Master.DTOs
{
    public class CreateAdvisedRequest
    {
        public string Text { get; set; } = string.Empty;
        public string? Category { get; set; }
        public bool IsCommon { get; set; } = true;
    }
}