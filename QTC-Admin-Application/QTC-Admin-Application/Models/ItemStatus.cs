using System;
using System.Collections.Generic;

namespace QTC_Admin_Application.Models;

public partial class ItemStatus
{
    public int ItemStatusId { get; set; }

    public string? StatusName { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; }

    public string? UpdatedBy { get; set; }

    public DateTime UpdatedDate { get; set; }

    public virtual ICollection<Item> Items { get; set; } = new List<Item>();
}
