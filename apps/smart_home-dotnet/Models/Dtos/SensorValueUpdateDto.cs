using System.ComponentModel.DataAnnotations;

namespace SmartHome.Api.Models.Dtos;

public class SensorValueUpdateDto
{
    [Required]
    public double? Value { get; set; }

    [Required]
    public string Status { get; set; } = string.Empty;
}
