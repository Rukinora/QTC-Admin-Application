using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation; // Add this using directive

namespace QTC_Admin_Application.Models;

public partial class WorkflowStep
{
    public int WorkflowStepId { get; set; }

    public int WorkflowId { get; set; }

    public string? StepName { get; set; }

    public string? StepDesc { get; set; }

    public string? StepType { get; set; }

    public string? StepSettings { get; set; }

    public string? Enabled { get; set; }

    public int Sequence { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; }

    public string? UpdatedBy { get; set; }

    public DateTime UpdatedDate { get; set; }

    public string? SearchConfig { get; set; }

    public string? ActionUrl { get; set; }

    public string? AuthUrlforAssignment { get; set; }

    [ValidateNever] // Tell the model binder to ignore this
    public virtual ICollection<ActivityLog> ActivityLogs { get; set; } = new List<ActivityLog>();

    [ValidateNever] // Tell the model binder to ignore this
    public virtual ICollection<Task> Tasks { get; set; } = new List<Task>();

    [ValidateNever] // Tell the model binder to ignore this
    public virtual Workflow Workflow { get; set; } = null!;
}