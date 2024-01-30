using System.Text.Json.Serialization;

namespace WoodgroveDemo.Models
{
    public class Constants
    {
        //
        public class AppSettings
        {
            public const int CACHE_EXPIRES_IN_MINUTES = 10;
        }
        public class DataView
        {
            public const string RequestPayload = "RequestPayload";
        }

        public class Endpoints
        {
            public const string CreateIssuanceRequest = "createIssuanceRequest";
            public const string CreatePresentationRequest = "createPresentationRequest";
        }

        public class ErrorMessages
        {
            public const string STATE_ID_NOT_FOUND = "Error the state ID couldn't be found in the system. Please try to refresh the page as start again.";

            public const string STATE_OBJECT_NOT_FOUND = "Error status object couldn't be found in the system. Please try to refresh the page as start again.";

            public const string STATE_ID_CANNOT_DESERIALIZE = "Error while trying to deserialize the status object. ";
            public const string API_ERROR = "Microsoft Entra verified ID API returned an error.";
            public const string API_CALLBACK_ENTRA_ERROR = "Microsoft Entra verified ID returned an error to callback endpoint.";
            public const string API_CALLBACK_INTERANL_ERROR = "Callback endpoint internal error.";

        }
        public class RequestStatus
        {
            public const string REQUEST_CREATED = "request_created";
            public const string REQUEST_RETRIEVED = "request_retrieved";
            public const string ISSUANCE_ERROR = "issuance_error";
            public const string ISSUANCE_SUCCESSFUL = "issuance_successful";
            public const string PRESENTATION_ERROR = "presentation_error";
            public const string PRESENTATION_VERIFIED = "presentation_verified";
            public const string SELFIE_TAKEN = "selfie_taken";
            public const string INVALID_REQUEST_STATUS = "invalid_request_status";
        }

        public class RequestStatusMessage
        {
            public const string REQUEST_CREATED = "To start, scan the QR code.";
            public const string REQUEST_RETRIEVED = "The QR code was successfully scanned. Waiting for you to complete the steps in your authenticator app.";
            public const string ISSUANCE_ERROR = "Issuance failed: ";
            public const string ISSUANCE_SUCCESSFUL = "The issuance successfully completed.";
            public const string PRESENTATION_ERROR = "Presentation failed: ";
            public const string PRESENTATION_VERIFIED = "The credential successfully verified.";
            public const string SELFIE_TAKEN = "The selfie successfully taken.";
            public const string INVALID_REQUEST_STATUS = "Cannot complete the process due to invalid request status.";

        }

    }
}

