//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="PowerDeviceEdge.cs" company="Microsoft Corporation">
//   Copyright (C) Microsoft Corporation.  All rights reserved.
//  </copyright>
//  --------------------------------------------------------------------------------------------------------------------

namespace KustoTest2.Models
{
    using DocDb;

    public class PowerDeviceEdge : IGremlinEdge
    {
        public PowerDeviceNode From { get; set; }
        public PowerDeviceNode To { get; set; }
        public DeviceAssociation Association { get; set; }

        public string GetId()
        {
            return $"{From.GetId()}-{To.GetId()}";
        }

        public string GetInVertexId()
        {
            return From.GetId();
        }

        public string GetOutVertexId()
        {
            return To.GetId();
        }

        public string GetLabel()
        {
            return Association.ToString();
        }

        public IGremlinVertex GetInVertex()
        {
            return From;
        }

        public IGremlinVertex GetOutVertex()
        {
            return To;
        }
    }
}