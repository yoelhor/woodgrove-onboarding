using System.Text.Json;
using System.Text.Json.Serialization;

namespace Woodgrove.Onboarding.Models;

public class UsersCache
{
    public string ID { get; set; }
    public string UPN { get; set; }
    public string DisplayName { get; set; }
    public string GivenName { get; set; }
    public string Surname { get; set; }
    public string Email { get; set; }
    public string ManagerEmail { get; set; }
    public string Status { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string Error { get; set; }
    public string Session { get; set; }
    public DateTime StatusTime { get; set; }


    /// <summary>
    /// Serialize this object into a string
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// Deserialize a JSON string into UsersCache object
    /// </summary>
    /// <param name="JsonString">The JSON string to be loaded</param>
    /// <returns></returns>
    public static UsersCache Parse(string JsonString)
    {
        return JsonSerializer.Deserialize<UsersCache>(JsonString);
    }
}