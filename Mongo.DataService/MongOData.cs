using System.Configuration;
using System.Data.Services;
using System.Data.Services.Common;
using Mongo.Context;
using Mongo.Context.Queryable;

namespace Mongo.DataService
{
    public class MongOData : MongoQueryableDataService
    {
        public MongOData()
            : base(Utils.BuildConnectionString(), ConfigurationManager.GetSection(MongoConfiguration.SectionName) as MongoConfiguration)
        {
        }

        public static void InitializeService(DataServiceConfiguration config)
        {
            config.SetEntitySetAccessRule("*", EntitySetRights.AllRead);
            config.SetServiceOperationAccessRule("*", ServiceOperationRights.All);
            config.DataServiceBehavior.MaxProtocolVersion = DataServiceProtocolVersion.V3;
            config.DataServiceBehavior.AcceptCountRequests = true;
            config.DataServiceBehavior.AcceptProjectionRequests = true;
            config.UseVerboseErrors = true;
        }
    }
}
