using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DataServiceProvider;

namespace Mongo.Context
{
    public interface IMongoDSPMetadata
    {
        DSPMetadata CreateMetadata(string connectionString);
    }
}
