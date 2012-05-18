using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataServiceProvider
{
    public class DSPQueryableContext : DSPContext
    {
        private DSPMetadata metadata;
        private Func<string, IQueryable> queryProviderFunc;

        public DSPQueryableContext(DSPMetadata metadata, Func<string, IQueryable> queryProviderFunc)
        {
            this.metadata = metadata;
            this.queryProviderFunc = queryProviderFunc;
        }

        public override IQueryable GetQueryable(string resourceSetName)
        {
            return this.queryProviderFunc(resourceSetName);
        }

        public override void AddResource(string resourceSetName, DSPResource resource)
        {
            throw new NotImplementedException();
        }

        public override void UpdateResource(string resourceSetName, DSPResource resource)
        {
            throw new NotImplementedException();
        }

        public override void RemoveResource(string resourceSetName, DSPResource resource)
        {
            throw new NotImplementedException();
        }
    }
}
