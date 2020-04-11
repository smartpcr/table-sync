//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="IGremlinEdge.cs" company="Microsoft Corporation">
//   Copyright (C) Microsoft Corporation.  All rights reserved.
//  </copyright>
//  --------------------------------------------------------------------------------------------------------------------

namespace KustoTest2.DocDb
{
    public interface IGremlinEdge
    {
        string GetId();
        string GetInVertexId();
        string GetOutVertexId();
        string GetLabel();

        IGremlinVertex GetInVertex();
        IGremlinVertex GetOutVertex();
    }
}