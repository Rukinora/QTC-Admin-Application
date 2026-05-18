using System;
using System.Collections.Generic;

namespace QTC_Admin_Application.Models;

public partial class ItemKeyValue
{
    public int ItemKeyValueId { get; set; }

    public int ItemId { get; set; }

    public int InfoKeyId { get; set; }

    public string? InfoKeyValue { get; set; }

    public string? Url { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; }

    public string? UpdatedBy { get; set; }

    public DateTime UpdatedDate { get; set; }

    public DateTime? InfoKeyDateTimeValue { get; set; }

    public virtual InfoKey InfoKey { get; set; } = null!;

    public virtual Item Item { get; set; } = null!;
}
