using System.ComponentModel.DataAnnotations.Schema;

namespace Zoolog.Models;

public class Location
{
    public int Id { get; set; }
    public string? Name { get; set; }
    [Column("latitude", TypeName = "decimal(18, 9)")]
    public decimal Latitude { get; set; }
    [Column("longitude", TypeName = "decimal(18, 9)")]
    public decimal Longitude { get; set; }
    public string? Country { get; set; }
    public string? Region { get; set; }

}