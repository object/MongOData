using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Services;
using System.Data.Services.Common;
using Mongo.Context;
using Mongo.Context.Queryable;

namespace SampleService
{
    public class MongoDataService : MongoQueryableDataService
    {
        public MongoDataService()
            : base(ConfigurationManager.ConnectionStrings["MongoDB"].ConnectionString, 
            ConfigurationManager.GetSection(MongoConfiguration.SectionName) as MongoConfiguration)
        {
        }

        // This method is called only once to initialize service-wide policies.
        public static void InitializeService(DataServiceConfiguration config)
        {
            // TODO: set rules to indicate which entity sets and service operations are visible, updatable, etc.
            config.SetEntitySetAccessRule("*", EntitySetRights.AllRead);
            config.SetServiceOperationAccessRule("*", ServiceOperationRights.All);
            config.DataServiceBehavior.MaxProtocolVersion = DataServiceProtocolVersion.V3;
            config.DataServiceBehavior.AcceptCountRequests = true;
            config.DataServiceBehavior.AcceptProjectionRequests = true;
            config.UseVerboseErrors = true;
        }
    }
}
