using System;
using System.Configuration;
using System.Web;

namespace Mongo.DataService
{
    internal class Utils
    {
        public static string BuildConnectionString()
        {
            var connectionString = ConfigurationManager.ConnectionStrings["MongoDB"].ConnectionString;
            int startIndex;
            int endIndex;
            string databaseName = ExtractDatabaseNameFromConnectionString(connectionString, out startIndex, out endIndex);
            if (databaseName == "*")
            {
                string path = HttpContext.Current.Request.Path.Substring(HttpContext.Current.Request.ApplicationPath.Length);
                databaseName = ExtractDatabaseNameFromRequestPath(path);
                if (!String.IsNullOrEmpty(databaseName))
                {
                    connectionString = connectionString.Substring(0, startIndex) + databaseName + 
                        (endIndex > 0 ? connectionString.Substring(endIndex) : string.Empty);
                }
            }
            return connectionString;
        }

        public static string ExtractServerNameFromConnectionString(string connectionString)
        {
            var hostIndex = connectionString.IndexOf("//");
            if (hostIndex > 0)
            {
                int endIndex = connectionString.IndexOf("/", hostIndex + 2) + 1;
                if (endIndex > 0)
                    return connectionString.Substring(0, endIndex);
                else
                    return connectionString;
            }
            return String.Empty;
        }

        public static string ExtractDatabaseNameFromConnectionString(string connectionString, out int startIndex, out int endIndex)
        {
            startIndex = -1;
            endIndex = -1;
            var hostIndex = connectionString.IndexOf("//");
            if (hostIndex > 0)
            {
                startIndex = connectionString.IndexOf("/", hostIndex + 2) + 1;
                endIndex = connectionString.IndexOf("?", startIndex);
                if (startIndex > 0)
                {
                    if (endIndex > 0)
                        return connectionString.Substring(startIndex, endIndex - startIndex);
                    else
                        return connectionString.Substring(startIndex);
                }
            }
            return String.Empty;
        }

        public static string ExtractDatabaseNameFromRequestPath(string requestPath)
        {
            var startIndex = requestPath.StartsWith("/") ? 1 : 0;
            var endIndex = requestPath.IndexOf("/", startIndex);
            if (endIndex > 0)
            {
                return requestPath.Substring(startIndex, endIndex - startIndex);
            }
            else
            {
                return requestPath.Substring(startIndex);
            }
        }
    }
}