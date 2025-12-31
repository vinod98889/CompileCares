using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileCares.Application.Features.Templates.DTOs
{
    public class UpdateTemplateItemsRequest
    {
        public List<TemplateComplaintRequest>? Complaints { get; set; }
        public List<TemplateMedicineRequest>? Medicines { get; set; }
        public List<TemplateAdvisedRequest>? AdvisedItems { get; set; }
    }
}
