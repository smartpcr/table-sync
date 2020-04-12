//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="CosmosDbRepoSettings.cs" company="Microsoft Corporation">
//   Copyright (C) Microsoft Corporation.  All rights reserved.
//  </copyright>
//  --------------------------------------------------------------------------------------------------------------------

namespace KustoTest2.DocDb
{
    using System;
    using Models;

    public class CosmosDbRepoSettings
    {
        [ModelBind(typeof(DataCenter))]
        public CosmosDbSettings DataCenter { get; set; }

        [GraphModelBind(typeof(PowerDeviceNode), typeof(PowerDeviceEdge))]
        public CosmosDbSettings DeviceGraph { get; set; }

        [ModelBind(typeof(PowerDevice))]
        public CosmosDbSettings Device { get; set; }

        [ModelBind(typeof(DeviceRelation))]
        public CosmosDbSettings DeviceAssociation { get; set; }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class ModelBindAttribute : Attribute
    {
        public Type ModelType { get; set; }

        public ModelBindAttribute(Type modelType)
        {
            ModelType = modelType;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class GraphModelBindAttribute : Attribute
    {
        public Type VertexType { get; set; }
        public Type EdgeType { get; set; }

        public GraphModelBindAttribute(Type vType, Type eType)
        {
            VertexType = vType;
            EdgeType = eType;
        }
    }
}