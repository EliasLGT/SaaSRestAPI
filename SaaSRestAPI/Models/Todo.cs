using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SaaSRestAPI.Models;

[Index("TodoName", Name = "UQ__Todos__1CD48C1BCAE15270", IsUnique = true)]
public partial class Todo
{
    [Key]
    [Column("TodoID")]
    public int TodoId { get; set; }

    [StringLength(32)]
    [Unicode(false)]
    public string TodoName { get; set; } = null!;

    public int? CreatedBy { get; set; }

    [ForeignKey("CreatedBy")]
    [InverseProperty("Todos")]
    public virtual User? CreatedByNavigation { get; set; }

    [InverseProperty("BelongsToNavigation")]
    public virtual ICollection<Item> Items { get; set; } = new List<Item>();
}
