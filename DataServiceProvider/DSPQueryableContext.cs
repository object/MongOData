

using System;
using System.Linq;

namespace DataServiceProvider
{
    public class DSPQueryableContext : DSPContext
    {
        private DSPMetadata _metadata;
        private Func<string, IQueryable> _createQueryProvider;

        public DSPQueryableContext(DSPMetadata metadata, Func<string, IQueryable> createQueryProvider)
        {
            _metadata = metadata;
            _createQueryProvider = createQueryProvider;
        }

        public override IQueryable GetQueryable(string resourceSetName)
        {
            return _createQueryProvider(resourceSetName);
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
