using System.Text.Json;
using System.Text.Json.Serialization;

namespace WoodgroveDemo.Models.Presentation
{

    /// <summary>
    /// VC Presentation
    /// </summary>
    public class PresentationRequest
    {
        public string authority { get; set; }
        public bool includeQRCode { get; set; }
        public Registration registration { get; set; }
        public Callback callback { get; set; }
        //public Presentation presentation { get; set; }
        public bool includeReceipt { get; set; }
        public List<RequestedCredential> requestedCredentials { get; set; }

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
    }

    /// <summary>
    /// Registration - used in both issuance and presentation to give the app a display name
    /// </summary>
    public class Registration
    {
        public string clientName { get; set; }
        public string purpose { get; set; }
    }

    /// <summary>
    /// Callback - defines where and how we want our callback.
    /// url - points back to us
    /// state - something we pass that we get back in the callback event. We use it as a correlation id
    /// headers - any additional HTTP headers you want to pass to the VC Client API. 
    /// The values you pass will be returned, as HTTP Headers, in the callback
    public class Callback
    {
        public string url { get; set; }
        public string state { get; set; }
        public Dictionary<string, string> headers { get; set; }
    }

    /// <summary>
    /// Presentation can involve asking for multiple VCs
    /// </summary>
    public class RequestedCredential
    {
        public string type { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string> acceptedIssuers { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Configuration configuration { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<Constraint> constraints { get; set; }

    }

    /// <summary>
    /// Configuration - presentation validation configuration
    /// </summary>
    public class Configuration
    {
        public Validation validation { get; set; }
    }
    /// <summary>
    /// Validation - presentation validation configuration
    /// </summary>
    public class Validation
    {
        public bool allowRevoked { get; set; } // default false

        public bool validateLinkedDomain { get; set; } // default false

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public FaceCheck faceCheck { get; set; }
    }

    /// <summary>
    /// FaceCheck - if to ask for face check and what claim + score you want
    /// </summary>
    public class FaceCheck
    {
        public string sourcePhotoClaimName { get; set; }
        public int matchConfidenceThreshold { get; set; }
    }

    public class Constraint
    {
        public string claimName { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string> values { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string contains { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string startsWith { get; set; }
    }
}