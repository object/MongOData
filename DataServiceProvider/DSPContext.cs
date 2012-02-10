//*********************************************************
//
//    Copyright (c) Microsoft. All rights reserved.
//    This code is licensed under the Microsoft Public License.
//    THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
//    ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
//    IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
//    PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************

namespace DataServiceProvider
{
    using System;
    using System.Data.Services.Providers;
    using System.Collections.Generic;

    /// <summary>The "context" for the DSP data provider. The context holds the actual data to be reported through the provider.</summary>
    /// <remarks>This implementation stores the data in a List and all of it is in-memory.</remarks>
    public class DSPContext
    {
        /// <summary>The actual data storage.</summary>
        /// <remarks>Dictionary where the key is the name of the resource set and the value is a list of resources.</remarks>
        private Dictionary<string, List<DSPResource>> resourceSetsStorage;

        /// <summary>Constructor, creates a new empty context.</summary>
        public DSPContext()
        {
            this.resourceSetsStorage = new Dictionary<string, List<DSPResource>>();
        }

        /// <summary>Gets a list of resources for the specified resource set.</summary>
        /// <param name="resourceSetName">The name of the resource set to get resources for.</param>
        /// <returns>List of resources for the specified resource set. Note that if such resource set was not yet seen by this context
        /// it will get created (with empty list).</returns>
        public IList<DSPResource> GetResourceSetEntities(string resourceSetName)
        {
            List<DSPResource> entities;
            if (!this.resourceSetsStorage.TryGetValue(resourceSetName, out entities))
            {
                entities = new List<DSPResource>();
                this.resourceSetsStorage[resourceSetName] = entities;
            }

            return entities;
        }
    }
}
