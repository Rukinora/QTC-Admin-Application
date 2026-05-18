using System;
using System.Collections.Generic;

namespace QTC_Admin_Application.Models;

public partial class TaskHistory
{
    public int TaskHistoryId { get; set; }

    public int ItemId { get; set; }

    public string? AssignedBy { get; set; }

    public DateTime? AssignedDate { get; set; }

    public string? AssignedTo { get; set; }

    public string? CompletedBy { get; set; }

    public DateTime? CompletedDate { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; }

    public string? IsPaused { get; set; }

    public DateTime? PausedDate { get; set; }

    public DateTime? ResumedDate { get; set; }

    public int StatusId { get; set; }

    public int Taskid { get; set; }

    public string? UpdatedBy { get; set; }

    public DateTime UpdatedDate { get; set; }

    public int WorkflowStepId { get; set; }

    public string? TaskRedirectUrl { get; set; }
}
