using System.Text.Json;

namespace woodgrove_portal.Controllers;

public class UsersCache
{
    public string UniqueID { get; set; }
    public string DisplayName { get; set; }
    public string EmployeeEmail { get; set; }
    public string ManagerEmail { get; set; }
    public string Status { get; set; }
    public string Error { get; set; }
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