using System;
using System.Collections.Generic;

namespace UnityMcpBridge.Editor.Helpers
{
    public static class Response
    {
        public static object Success(string message, object data = null)
        {
            var result = new Dictionary<string, object>
            {
                { "message", message }
            };
            
            if (data != null)
            {
                result["data"] = data;
            }
            
            return new
            {
                status = "success",
                result = result
            };
        }

        public static object Error(string errorMessage, object data = null)
        {
            // エラーの場合はerrorフィールドを使用
            var response = new Dictionary<string, object>
            {
                { "status", "error" },
                { "error", errorMessage }
            };
            
            if (data != null)
            {
                response["data"] = data;
            }
            
            return response;
        }
    }
}
