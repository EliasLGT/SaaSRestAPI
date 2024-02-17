using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SaaSRestAPI.Models;

public partial class Item
{
    [Key]
    [Column("ItemID")]
    public int ItemId { get; set; }

    [StringLength(45)]
    [Unicode(false)]
    public string ItemName { get; set; } = null!;

    public bool Done { get; set; }

    public int? BelongsTo { get; set; }

    [ForeignKey("BelongsTo")]
    [InverseProperty("Items")]
    public virtual Todo? BelongsToNavigation { get; set; }
}
