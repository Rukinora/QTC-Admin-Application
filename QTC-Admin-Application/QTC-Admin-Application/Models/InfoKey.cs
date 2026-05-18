using System;
using System.Collections.Generic;

namespace QTC_Admin_Application.Models;

public partial class InfoKey
{
    public int InfoKeyId { get; set; }

    public string? InfoKeyName { get; set; }

    public int WorkflowId { get; set; }

    public string? ManagerDisplay { get; set; }

    public string? UserDisplay { get; set; }

    public int? DisplayOrder { get; set; }

    public string? SearchType { get; set; }

    public string? DisplayName { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; }

    public string? UpdatedBy { get; set; }

    public DateTime UpdatedDate { get; set; }

    public string? DataType { get; set; }

    public virtual ICollection<ItemKeyValue> ItemKeyValues { get; set; } = new List<ItemKeyValue>();

    public virtual Workflow Workflow { get; set; } = null!;
}
