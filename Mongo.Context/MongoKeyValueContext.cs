using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DataServiceProvider;

namespace Mongo.Context
{
    public class MongoKeyValueContext : IMongoDSPContext
    {
        public DSPContext CreateContext(DSPMetadata metadata, string connectionString)
        {
            return new DSPContext();
        }
    }
}
