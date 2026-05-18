using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QTC_Admin_Application.Models;

public partial class Workflow
{
    public int WorkflowId { get; set; }
    public string? WorkflowName { get; set; }
    public string? WorkflowType { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; }
    public int RecordLimit { get; set; }
    public int? SubStatusId { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedDate { get; set; }

    // Correctly typed navigation properties
    public virtual ICollection<ActivityLog> ActivityLogs { get; set; } = new List<ActivityLog>();
    public virtual ICollection<InfoKey> InfoKeys { get; set; } = new List<InfoKey>();
    public virtual ICollection<Item> Items { get; set; } = new List<Item>();
    public virtual ICollection<WorkflowStep> WorkflowSteps { get; set; } = new List<WorkflowStep>();
}
