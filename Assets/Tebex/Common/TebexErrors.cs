using System;
using System.Collections.Generic;

namespace Tebex.Common
{
    /// <summary>
    /// Represents an error returned by the Headless API.
    /// </summary>
    [Serializable]
    public class HeadlessApiError
    {
        public string status = string.Empty;
        
        public string type = string.Empty;
        
        public string title = string.Empty;
        
        public string detail = string.Empty;

        public string error_code = string.Empty;
        
        public List<string> field_details = new List<string>();
        
        public List<string> meta = new List<string>();
        
        public Exception AsException() {
            return new Exception(Json.SerializeObject(this));
        }
    }

    /// <summary>
    /// Represents a JSON error returned by the Plugin API.
    /// </summary>
    [Serializable]
    public class PluginApiError
    {
        public int error_code;
        public string error_message = string.Empty;
    }
    
    /// <summary>
    /// Represents a non-JSON error response code and body, typically error 500 or something else unexpected.
    /// </summary>
    public class ServerError
    {
        /// <summary>
        /// The Http response code
        /// </summary>
        public int Code;
        
        /// <summary>
        /// The Http response body
        /// </summary>
        public string Body;

        public ServerError(int code, string body)
        {
            Code = code;
            Body = body;
        }

        public Exception AsException()
        {
            return new Exception("Unexpected server error (" + Code + "): " + Body);
        }
    }
}