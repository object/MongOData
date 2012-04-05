using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Services;
using System.Data.Services.Common;
using System.Text.RegularExpressions;
using System.Web;
using Mongo.Context;
using Mongo.Context.Queryable;

namespace Mongo.DataService
{
    public class MongOData : MongoQueryableDataService
    {
        public MongOData()
            : base(BuildConnectionString())
        {
        }

        public static void InitializeService(DataServiceConfiguration config)
        {
            config.SetEntitySetAccessRule("*", EntitySetRights.AllRead);
            config.SetServiceOperationAccessRule("*", ServiceOperationRights.All);
            config.DataServiceBehavior.MaxProtocolVersion = DataServiceProtocolVersion.V2;
            config.DataServiceBehavior.AcceptCountRequests = true;
            config.DataServiceBehavior.AcceptProjectionRequests = true;
            config.UseVerboseErrors = true;
        }

        private static string BuildConnectionString()
        {
            var connectionString = ConfigurationManager.ConnectionStrings["MongoDB"].ConnectionString;
            int startIndex;
            int endIndex;
            string databaseName = ExtractDatabaseNameFromConnectionString(connectionString, out startIndex, out endIndex);
            if (databaseName == "*")
            {
                databaseName = ExtractDatabaseNameFromRequestPath(HttpContext.Current.Request.Path);
                if (!string.IsNullOrEmpty(databaseName))
                {
                    connectionString = connectionString.Substring(0, startIndex) + databaseName + connectionString.Substring(endIndex);
                }
            }
            return connectionString;
        }

        private static string ExtractDatabaseNameFromConnectionString(string connectionString, out int startIndex, out int endIndex)
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
            return string.Empty;
        }

        private static string ExtractDatabaseNameFromRequestPath(string requestPath)
        {
            if (requestPath.StartsWith("/"))
            {
                var startIndex = 1;
                var endIndex = requestPath.IndexOf("/", 1);
                if (endIndex > 0)
                {
                    return requestPath.Substring(startIndex, endIndex - startIndex);
                }
            }
            return string.Empty;
        }
    }
}
