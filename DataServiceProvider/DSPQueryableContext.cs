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
        private Func<string, IQueryable> createQueryProvider;

        public DSPQueryableContext(DSPMetadata metadata, Func<string, IQueryable> createQueryProvider)
        {
            this.metadata = metadata;
            this.createQueryProvider = createQueryProvider;
        }

        public override IQueryable GetQueryable(string resourceSetName)
        {
            return this.createQueryProvider(resourceSetName);
        }

        public override void AddResource(string resourceSetName, DSPResource resource)
        {
        }

        public override void UpdateResource(string resourceSetName, DSPResource resource)
        {
        }

        public override void RemoveResource(string resourceSetName, DSPResource resource)
        {
        }
    }
}
