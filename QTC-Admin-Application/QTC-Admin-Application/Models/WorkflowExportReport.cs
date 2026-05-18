namespace QTC_Admin_Application.Models
{
    public class WorkflowExportReport
    {
        public Workflow Workflow { get; set; } = null!;
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public List<WorkflowExportRow> Rows { get; set; } = new();
        public DateTime GeneratedAt { get; set; } = DateTime.Now;
        public string GeneratedBy { get; set; } = "SYSTEM";
    }
}