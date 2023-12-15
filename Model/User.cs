using System.Text.Json.Serialization;

namespace backendASPNET.Model;

public class User
{
    public int Id { get; set; }
    public string Login { get; set; } = null!;
    public string Password { get; set; } = null!;
    [JsonIgnore]
    public virtual ICollection<BusStop> FavoriteBusStops { get; set; } = null!;
}
