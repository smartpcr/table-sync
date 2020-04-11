//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="IGraphDbClient.cs" company="Microsoft Corporation">
//   Copyright (C) Microsoft Corporation.  All rights reserved.
//  </copyright>
//  --------------------------------------------------------------------------------------------------------------------

namespace KustoTest2.DocDb
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IGraphDbClient<V, E>
        where V : class, IGremlinVertex, new()
        where E : class, IGremlinEdge, new()
    {
        Task BulkInsertVertices(IEnumerable<V> vertices, string partition, CancellationToken cancel);
        Task BulkInsertEdges(IEnumerable<E> edges, string partition, CancellationToken cancel);
    }
}