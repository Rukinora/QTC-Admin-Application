namespace QTC_Admin_Application.Models
{
    public class WorkflowExportRow
    {
        public int TaskId { get; set; }
        public int WorkflowStepId { get; set; }
        public string WorkflowStepName { get; set; } = "";
        public string StatusName { get; set; } = "";
        public string SubStatusName { get; set; } = "";
        public string AssignedTo { get; set; } = "";
        public string AssignedBy { get; set; } = "";
        public string CompletedBy { get; set; } = "";
        public DateTime CreatedDate { get; set; }
        public DateTime? AssignedDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}