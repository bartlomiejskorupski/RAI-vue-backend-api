using System.Text.Json.Serialization;

namespace backendASPNET.Model;

public class BusStop
{
    public int Id { get; set; }
    [JsonIgnore]
    public virtual User User { get; set; } = null!;
    public int StopId { get; set; }
    public string Name { get; set; } = null!;
}
