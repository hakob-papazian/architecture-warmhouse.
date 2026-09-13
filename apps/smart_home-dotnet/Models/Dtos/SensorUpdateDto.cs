namespace SmartHome.Api.Models.Dtos;

public class SensorUpdateDto
{
    public string? Name { get; set; }

    public string? Type { get; set; }

    public string? Location { get; set; }

    public double? Value { get; set; }

    public string? Unit { get; set; }

    public string? Status { get; set; }
}
