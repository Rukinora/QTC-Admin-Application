using System.Collections.Generic;

namespace QTC_Admin_Application.Models
{
    public class WorkloadDashboardViewModel
    {
        public List<WorkloadUserViewModel> Users { get; set; } = new();
        public string? SelectedUserName { get; set; }
        public List<MyTaskRowViewModel> SelectedUserTasks { get; set; } = new();
    }

    public class WorkloadUserViewModel
    {
        public string UserName { get; set; } = "";
        public int TaskCount { get; set; }
    }
}
