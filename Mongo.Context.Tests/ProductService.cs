using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Services;
using System.Data.Services.Providers;
using System.Linq;
using System.Text;
using DataServiceProvider;
using Mongo.Context.InMemory;
using Mongo.Context.Queryable;
using MongoDB.Driver;

namespace Mongo.Context.Tests
{
    public class ProductInMemoryService : MongoInMemoryDataService
    {
        public ProductInMemoryService() 
            : base (ConfigurationManager.ConnectionStrings["MongoDB"].ConnectionString, TestService.Configuration)
        {
        }

        public static void InitializeService(DataServiceConfiguration config)
        {
            config.SetEntitySetAccessRule("*", EntitySetRights.All);
            config.DataServiceBehavior.MaxProtocolVersion = System.Data.Services.Common.DataServiceProtocolVersion.V3;
            config.DataServiceBehavior.AcceptCountRequests = true;
            config.DataServiceBehavior.AcceptProjectionRequests = true;
            config.UseVerboseErrors = true;
        }
    }

    public class ProductQueryableService : MongoQueryableDataService
    {
        public ProductQueryableService()
            : base(ConfigurationManager.ConnectionStrings["MongoDB"].ConnectionString, TestService.Configuration)
        {
        }

        public static void InitializeService(DataServiceConfiguration config)
        {
            config.SetEntitySetAccessRule("*", EntitySetRights.All);
            config.DataServiceBehavior.MaxProtocolVersion = System.Data.Services.Common.DataServiceProtocolVersion.V3;
            config.DataServiceBehavior.AcceptCountRequests = true;
            config.DataServiceBehavior.AcceptProjectionRequests = true;
            config.UseVerboseErrors = true;
        }
    }
}
