using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileCares.Application.Features.Consultations.DTOs
{
    public class TodaysStatsDto
    {
        public DateTime Date { get; set; }
        public int ConsultationCount { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}
