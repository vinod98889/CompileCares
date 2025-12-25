namespace CompileCares.Application.Features.Templates.DTOs
{
    public class TemplateStatisticsDto
    {
        public int TotalTemplates { get; set; }
        public int PublicTemplates { get; set; }
        public int PrivateTemplates { get; set; }

        public int MostUsedTemplateId { get; set; }
        public string MostUsedTemplateName { get; set; } = string.Empty;
        public int MostUsedTemplateCount { get; set; }

        public Dictionary<string, int> TemplatesByCategory { get; set; } = new();
        public Dictionary<string, int> TemplatesByDoctor { get; set; } = new();

        public int TemplatesWithMedicines { get; set; }
        public int TemplatesWithComplaints { get; set; }
        public int TemplatesWithAdvice { get; set; }

        public decimal AverageMedicinesPerTemplate { get; set; }
        public decimal AverageComplaintsPerTemplate { get; set; }
        public decimal AverageAdvicePerTemplate { get; set; }
    }
}