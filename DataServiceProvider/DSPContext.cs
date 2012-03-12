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

using System.Linq;

namespace DataServiceProvider
{
    using System;
    using System.Data.Services.Providers;
    using System.Collections.Generic;

    public abstract class DSPContext
    {
        public abstract IQueryable GetQueryable(string resourceSetName);
        public abstract void AddResource(string resourceSetName, DSPResource resource);
        public abstract void UpdateResource(string resourceSetName, DSPResource resource);
        public abstract void RemoveResource(string resourceSetName, DSPResource resource);
    }
}
