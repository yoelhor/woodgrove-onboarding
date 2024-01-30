using System.Text.Json;
using System.Text.Json.Serialization;

namespace WoodgroveDemo.Models.Presentation;
public class PresentationResponse
{
    public string requestId { get; set; }
    public string url { get; set; }
    public int expiry { get; set; }
    public string id { get; set; }

    /// <summary>
    /// Serialize this object into a string
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// Serialize this object into HTML string
    /// </summary>
    /// <returns></returns>
    public string ToHtml()
    {
        return this.ToString().Replace("\r\n", "<br>").Replace(" ", "&nbsp;");
    }

    /// <summary>
    /// Deserialize a JSON string into PresentationResponse object
    /// </summary>
    /// <param name="JsonString">The JSON string to be loaded</param>
    /// <returns></returns>
    public static PresentationResponse Parse(string JsonString)
    {
        return JsonSerializer.Deserialize<PresentationResponse>(JsonString);
    }
}