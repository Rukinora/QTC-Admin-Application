using System.Collections.Generic;

namespace QTC_Admin_Application.Models
{
    public class MyTasksViewModel
    {
        public List<MyTaskRowViewModel> Tasks { get; set; } = new();
    }

    public class MyTaskRowViewModel
    {
        public int TaskId { get; set; }
        public int WorkflowId { get; set; }
        public string WorkflowName { get; set; } = "";
        public string StepName { get; set; } = "";
        public string StatusName { get; set; } = "";
        public string ItemKey { get; set; } = "";
        public DateTime UpdatedDate { get; set; }
    }
}
