

using System.Text.Json;
using System.Text.Json.Serialization;

namespace WoodgroveDemo.Models;

public class ResponseError
{
    public string requestId { get; set; }
    public string date { get; set; }
    public string mscv { get; set; }
    public ResponseOuterError error { get; set; }

    public string GetUserMessage()
    {
        if (error != null && error.innererror != null)
        {
            return error.innererror.message;
        }

        if (error != null)
        {
            return error.message;
        }

        return this.ToString();
    }


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
    public static ResponseError Parse(string JsonString)
    {
        return JsonSerializer.Deserialize<ResponseError>(JsonString);
    }
}

public class ResponseOuterError
{
    public string code { get; set; }
    public string message { get; set; }
    public ResponseInnererror innererror { get; set; }
}

public class ResponseInnererror
{
    public string code { get; set; }
    public string message { get; set; }
    public string target { get; set; }
}
