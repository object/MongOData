using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DataServiceProvider;

namespace Mongo.Context
{
    public interface IMongoDSPContext
    {
        DSPContext CreateContext(DSPMetadata metadata, string connectionString);
    }
}
