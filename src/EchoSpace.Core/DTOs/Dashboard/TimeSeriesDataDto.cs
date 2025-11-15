namespace EchoSpace.Core.DTOs.Dashboard;

public class TimeSeriesDataDto
{
    public List<TimeSeriesPointDto> Data { get; set; } = new();
}

public class TimeSeriesPointDto
{
    public DateTime Date { get; set; }
    public int Value { get; set; }
    public string? Label { get; set; }
}

