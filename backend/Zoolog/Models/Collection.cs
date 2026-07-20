using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Zoolog.Models;

public class Collection
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsPublic { get; set; } = false;
    public int CreatedBy { get; set; }
    [JsonIgnore]
    [ValidateNever]
    public User Creator { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    
}