using CompileCares.Application.Common.DTOs;

namespace CompileCares.Application.Features.Patients.DTOs
{
    public class PatientSearchResponse : PagedResponse<PatientSummaryDto>
    {
        public PatientSearchResponse(
            IEnumerable<PatientSummaryDto> data,
            int pageNumber,
            int pageSize,
            int totalRecords)
            : base(data, pageNumber, pageSize, totalRecords)
        {
        }
    }
}