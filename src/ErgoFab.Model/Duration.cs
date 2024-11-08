using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ErgoFab.Model;

[Owned]
public class Duration
{
    [Column(TypeName = "datetimeoffset(2)")] /* otherwise datetimeoffset(7) 10 bytes */
    public DateTimeOffset Start { get; set; }
    [Column(TypeName = "datetimeoffset(2)")]
    public DateTimeOffset? Finish { get; set; }
}