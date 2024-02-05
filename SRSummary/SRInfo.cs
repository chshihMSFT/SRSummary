using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRSummary
{
    public class SRInfo
    {
        public string IncidentId { get; set; }
        public string Title { get; set; }
        public string IssueDescription { get; set; }
        public string Symptomstxt { get; set; }
        public string GPTIssueSummary { get; set; }
    }
}
