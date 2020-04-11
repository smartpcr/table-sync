//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="IGremlinVertex.cs" company="Microsoft Corporation">
//   Copyright (C) Microsoft Corporation.  All rights reserved.
//  </copyright>
//  --------------------------------------------------------------------------------------------------------------------

namespace KustoTest2.DocDb
{
    public interface IGremlinVertex
    {
        string GetId();
        string GetPartition();
        string GetLabel();
    }
}