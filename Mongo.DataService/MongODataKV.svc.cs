using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Services;
using System.Data.Services.Common;
using Mongo.Context;

namespace Mongo.DataService
{
    public class MongODataKV : MongoDataService<MongoKeyValueContext, MongoKeyValueMetadata>
    {
        public MongODataKV()
            : base(ConfigurationManager.ConnectionStrings["MongoDB"].ConnectionString)
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
    }
}
