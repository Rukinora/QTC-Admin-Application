using System;
using System.Collections.Generic;

namespace QTC_Admin_Application.Models;

public partial class Task
{
    public int TaskId { get; set; }

    public string? AssignedBy { get; set; }

    public DateTime? AssignedDate { get; set; }

    public string? AssignedTo { get; set; }

    public string? CompletedBy { get; set; }

    public DateTime? CompletedDate { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; }

    public string? IsPaused { get; set; }

    public int ItemId { get; set; }

    public DateTime? PausedDate { get; set; }

    public DateTime? ResumedDate { get; set; }

    public int StatusId { get; set; }

    public int? SubStatusId { get; set; }

    public string? UpdatedBy { get; set; }

    public DateTime UpdatedDate { get; set; }

    public byte[]? VerCol { get; set; }

    public int WorkflowStepId { get; set; }

    public int? BasePriority { get; set; }

    public int? AgingFactor { get; set; }

    public int? AgingPeriod { get; set; }

    public string? TaskRedirectUrl { get; set; }

    public DateTime? DelayUntil { get; set; }

    public bool? ShowAsNew { get; set; }

    public int? DelayHours { get; set; }

    public virtual Status Status { get; set; } = null!;

    public virtual SubStatus? SubStatus { get; set; }

    public virtual WorkflowStep WorkflowStep { get; set; } = null!;

    public virtual Item Item { get; set; } = null!;
}
