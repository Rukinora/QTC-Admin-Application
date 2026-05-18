using System;
using System.Collections.Generic;

namespace QTC_Admin_Application.Models;

public partial class SearchView
{
    public int WorkflowId { get; set; }

    public string? WorkflowName { get; set; }

    public int WorkflowStepId { get; set; }

    public string? WorkflowStepName { get; set; }

    public string? WorkflowStepType { get; set; }

    public string? WorkflowStepEnabled { get; set; }

    public int Sequence { get; set; }

    public int ItemId { get; set; }

    public string? ItemKey { get; set; }

    public int TaskId { get; set; }

    public int StatusId { get; set; }

    public string? AssignedTo { get; set; }

    public string? IsPaused { get; set; }

    public string? AssignedBy { get; set; }

    public DateTime? AssignedDate { get; set; }

    public string? CompletedBy { get; set; }

    public DateTime? CompletedDate { get; set; }

    public string? TaskRedirectUrl { get; set; }

    public bool? ShowAsNew { get; set; }

    public DateTime? PausedDate { get; set; }

    public DateTime? ResumedDate { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; }

    public string? UpdatedBy { get; set; }

    public DateTime UpdatedDate { get; set; }

    public byte[]? VerCol { get; set; }

    public string? StatusName { get; set; }

    public int? SubStatusId { get; set; }

    public int ItemKeyValueId { get; set; }

    public string? InfoKeyValue { get; set; }

    public string? Url { get; set; }

    public DateTime? InfoKeyDateTimeValue { get; set; }

    public int InfoKeyId { get; set; }

    public string? InfoKeyName { get; set; }

    public string? ManagerDisplay { get; set; }

    public string? UserDisplay { get; set; }

    public string? DataType { get; set; }
}
