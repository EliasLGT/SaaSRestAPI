using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SaaSRestAPI.Models;

[Index("UserName", Name = "UQ__Users__C9F2845606F61100", IsUnique = true)]
public partial class User
{
    [Key]
    [Column("UserID")]
    public int UserId { get; set; }

    [StringLength(32)]
    [Unicode(false)]
    public string UserName { get; set; } = null!;

    [StringLength(32)]
    [Unicode(false)]
    public string Password { get; set; } = null!;

    [InverseProperty("CreatedByNavigation")]
    public virtual ICollection<Todo> Todos { get; set; } = new List<Todo>();
}
