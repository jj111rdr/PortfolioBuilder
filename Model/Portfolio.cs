using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Portfolio_Builder.Model;

[Index("PortfolioLink", Name = "UQ_Portfolios_PortfolioLink", IsUnique = true)]
[Index("Username", Name = "UQ_Portfolios_Username", IsUnique = true)]
public partial class Portfolio
{
    [Key]
    public Guid Id { get; set; }

    [StringLength(39)]
    public string Username { get; set; } = null!;

    [StringLength(201)]
    public string FullName { get; set; } = null!;

    [StringLength(256)]
    public string PortfolioLink { get; set; } = null!;

    [Precision(3)]
    public DateTime CreatedAt { get; set; }

    [ForeignKey("Username")]
    [InverseProperty("Portfolio")]
    public virtual User UsernameNavigation { get; set; } = null!;
}
