using System;
using System.Collections.Generic;

namespace QTC_Admin_Application.Models;

public partial class ActivityLog
{
    public int ActivityLogId { get; set; }

    public int TaskId { get; set; }

    public int WorkflowId { get; set; }

    public int WorkflowStepId { get; set; }

    public int StatusId { get; set; }

    public string ActivityUser { get; set; } = null!;

    public string Action { get; set; } = null!;

    public string? Operation { get; set; }

    public string CreatedBy { get; set; } = null!;

    public DateTime CreatedDate { get; set; }

    public string UpdatedBy { get; set; } = null!;

    public DateTime UpdatedDate { get; set; }

    public virtual Status Status { get; set; } = null!;

    public virtual Workflow Workflow { get; set; } = null!;

    public virtual WorkflowStep WorkflowStep { get; set; } = null!;
}
