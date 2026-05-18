using System;
using System.Collections.Generic;

namespace QTC_Admin_Application.Models;

public partial class SubStatus
{
    public int SubStatusId { get; set; }

    public int WorkflowId { get; set; }

    public int WorkflowStepId { get; set; }

    public string? SubStatusName { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; }

    public string? UpdatedBy { get; set; }

    public DateTime UpdatedDate { get; set; }

    public bool? HoldTrigger { get; set; }

    public virtual ICollection<Task> Tasks { get; set; } = new List<Task>();
}
