using System.ComponentModel.DataAnnotations;

namespace SmartHome.Api.Models.Dtos;

public class SensorCreateDto
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Type { get; set; } = string.Empty;

    [Required]
    public string Location { get; set; } = string.Empty;

    public string? Unit { get; set; }
}
