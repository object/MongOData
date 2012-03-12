using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataServiceProvider
{
    public class DSPInMemoryContext :  DSPContext
    {
        /// <summary>The actual data storage.</summary>
        /// <remarks>Dictionary where the key is the name of the resource set and the value is a list of resources.</remarks>
        private Dictionary<string, List<DSPResource>> resourceSetsStorage;

        /// <summary>Constructor, creates a new empty context.</summary>
        public DSPInMemoryContext()
        {
            this.resourceSetsStorage = new Dictionary<string, List<DSPResource>>();
        }

        public List<DSPResource> GetResourceSetStorage(string resourceSetName)
        {
            List<DSPResource> storage;
            if (this.resourceSetsStorage.TryGetValue(resourceSetName, out storage))
            {
                return storage;
            }
            else
            {
                storage = new List<DSPResource>();
                this.resourceSetsStorage.Add(resourceSetName, storage);
                return storage;
            }
        }

        /// <summary>Gets a list of resources for the specified resource set.</summary>
        /// <param name="resourceSetName">The name of the resource set to get resources for.</param>
        /// <returns>List of resources for the specified resource set. Note that if such resource set was not yet seen by this context
        /// it will get created (with empty list).</returns>
        public override IQueryable GetQueryable(string resourceSetName)
        {
            List<DSPResource> entities;
            if (!this.resourceSetsStorage.TryGetValue(resourceSetName, out entities))
            {
                entities = new List<DSPResource>();
                this.resourceSetsStorage[resourceSetName] = entities;
            }

            return entities.AsQueryable();
        }

        public override void AddResource(string resourceSetName, DSPResource resource)
        {
            this.GetResourceSetStorage(resourceSetName).Add(resource);
        }

        public override void UpdateResource(string resourceSetName, DSPResource resource)
        {
        }

        public override void RemoveResource(string resourceSetName, DSPResource resource)
        {
            this.GetResourceSetStorage(resourceSetName).Remove(resource);
        }
    }
}
