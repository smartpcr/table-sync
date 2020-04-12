//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="IGremlinVertex.cs" company="Microsoft Corporation">
//   Copyright (C) Microsoft Corporation.  All rights reserved.
//  </copyright>
//  --------------------------------------------------------------------------------------------------------------------

namespace KustoTest2.DocDb
{
    using System.Collections.Generic;
    using System.Reflection;

    public interface IGremlinVertex
    {
        string Id { get; set; }
        string PartitionKey { get; set; }
        string Label { get; set; }
        List<PropertyInfo> GetFlattenedProperties();
        Dictionary<string, string> GetPropertyValues(List<PropertyInfo> props);
    }
}