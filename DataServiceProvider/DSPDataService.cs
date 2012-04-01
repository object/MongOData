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
    using System.Data.Services;
    using System.Data.Services.Providers;

    /// <summary>Data service implementation which can defined metadata for the service and stores the data as property bags.</summary>
    /// <typeparam name="T">The type of the context to use. This must derive from the <see cref="DSPContext"/> class.</typeparam>
    public abstract class DSPDataService<T,Q> : DataService<T>, IServiceProvider where T : DSPContext where Q : DSPResourceQueryProvider, new()
    {
        /// <summary>The metadata definition. This also provides the <see cref="IDataServiceMetadataProvider"/> implementation.</summary>
        protected DSPMetadata metadata;

        /// <summary>The resource query provider implementation for the service. Implements <see cref="IDataServiceQueryProvider"/>.</summary>
        protected DSPResourceQueryProvider resourceQueryProvider;

        /// <summary>Abstract method which a derived class implements to create the metadata for the service.</summary>
        /// <returns>The metadata definition for the service. Note that this is called only once per the service lifetime.</returns>
        protected abstract DSPMetadata CreateDSPMetadata();

        /// <summary>Returns the metadata definition for the service. It will create it if no metadata is available yet.</summary>
        protected DSPMetadata Metadata
        {
            get
            {
                if (this.metadata == null)
                {
                    this.metadata = CreateDSPMetadata();
                    this.metadata.SetReadOnly();
                    this.resourceQueryProvider = new Q();
                    this.resourceQueryProvider.Metadata = this.metadata;
                }

                return this.metadata;
            }
        }

        #region IServiceProvider Members

        /// <summary>Returns service implementation.</summary>
        /// <param name="serviceType">The type of the service requested.</param>
        /// <returns>Implementation of such service or null.</returns>
        public virtual object GetService(Type serviceType)
        {
            if (serviceType == typeof(IDataServiceMetadataProvider))
            {
                return this.Metadata;
            }
            else if (serviceType == typeof(IDataServiceQueryProvider))
            {
                return this.resourceQueryProvider;
            }
            else if (serviceType == typeof(IDataServiceUpdateProvider))
            {
                return new DSPUpdateProvider(this.CurrentDataSource, this.Metadata);
            }
            else
            {
                return null;
            }
        }

        #endregion
    }
}
