using System.Text.Json;
using System.Text.Json.Serialization;

namespace WoodgroveDemo.Models;

/// <summary>
/// Verified ID Client API callback
/// </summary>
public class CallbackEvent
{
    public string requestId { get; set; }
    public string requestStatus { get; set; }
    public Error error { get; set; }
    public string state { get; set; }
    public string subject { get; set; }
    public List<VerifiedCredentialsData> verifiedCredentialsData { get; set; }
    public Receipt receipt { get; set; }
    public string photo { get; set; }

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
    /// Deserialize a JSON string into CallbackEvent object
    /// </summary>
    /// <param name="JsonString">The JSON string to be loaded</param>
    /// <returns></returns>
    public static CallbackEvent Parse(string JsonString)
    {
        return JsonSerializer.Deserialize<CallbackEvent>(JsonString);
    }

}

/// <summary>
/// Receipt - returned when VC presentation is verified. The id_token contains the full VC id_token
/// the state is not to be confused with the VCCallbackEvent.state and is something internal to the VC Client API
/// </summary>
public class Receipt
{
    // [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    // public string id_token { get; set; }

    // [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    // public string vp_token { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string state { get; set; }
}


/// <summary>
/// Error - in case the VC Client API returns an error
/// </summary>
public class Error
{
    public string code { get; set; }
    public string message { get; set; }
}

public class Innererror
{
    public string code { get; set; }
    public string message { get; set; }
    public string target { get; set; }
}

/// <summary>
/// ClaimsIssuer - details of each VC that was presented (usually just one)
/// authority gives you who issued the VC and the claims is a collection of the VC's claims, like givenName, etc
/// </summary>
// public class ClaimsIssuer
// {
//     public string issuer { get; set; }
//     public string domain { get; set; }
//     public string verified { get; set; }
//     public string[] type { get; set; }
//     public IDictionary<string, string> claims { get; set; }
//     public CredentialState credentialState { get; set; }
//     public FaceCheckResult faceCheck { get; set; }
//     public DomainValidation domainValidation { get; set; }
//     public string expirationDate { get; set; }
//     public string issuanceDate { get; set; }
// }

public class VerifiedCredentialsData
{
    public string issuer { get; set; }
    public List<string> type { get; set; }
    public Claims claims { get; set; }
    public CredentialState credentialState { get; set; }
    public DomainValidation domainValidation { get; set; }
    public DateTime expirationDate { get; set; }
    public DateTime issuanceDate { get; set; }
}

public class CredentialState
{
    public string revocationStatus { get; set; }
    [JsonIgnore]
    public bool isValid { get { return revocationStatus == "VALID"; } }
}

public class DomainValidation
{
    public string url { get; set; }
}

public class FaceCheckResult
{
    public double matchConfidenceScore { get; set; }
}

public class Claims
{
    public string firstName { get; set; }
    public string lastName { get; set; }
    public string id { get; set; }
    public string sum { get; set; }
    public string displayName { get; set; }
}