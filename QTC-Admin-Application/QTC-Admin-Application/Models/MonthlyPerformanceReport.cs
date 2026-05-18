namespace QTC_Admin_Application.Models
{
    public class MonthlyPerformanceReport
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        public int TotalWorkflowsCreated { get; set; }
        public int TotalWorkflowsCompleted { get; set; }
        public int TotalWorkflowsPending { get; set; }

        public double AverageCompletionDays { get; set; }
    }
}
