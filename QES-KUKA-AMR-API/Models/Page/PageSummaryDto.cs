using System.ComponentModel.DataAnnotations;

namespace QES_KUKA_AMR_API.Models.Page
{
    public class PageSummaryDto
    {
        public int Id { get; set; }
        public string PageName { get; set; } = string.Empty;
        public string PagePath { get; set; } = string.Empty;
    }
}
