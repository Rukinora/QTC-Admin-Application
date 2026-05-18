using System;
using System.Collections.Generic;

namespace QTC_Admin_Application.Models;

public partial class Item
{
    public int ItemId { get; set; }

    public int WorkflowId { get; set; }

    public int CurrentWorkflowStepId { get; set; }

    public int ItemStatusId { get; set; }

    public string? ItemKey { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; }

    public string? UpdatedBy { get; set; }

    public DateTime UpdatedDate { get; set; }

    public byte[]? VerCol { get; set; }

    public string? Payload { get; set; }

    public virtual ICollection<ItemKeyValue> ItemKeyValues { get; set; } = new List<ItemKeyValue>();

    public virtual ItemStatus ItemStatus { get; set; } = null!;

    public virtual Workflow Workflow { get; set; } = null!;
}
