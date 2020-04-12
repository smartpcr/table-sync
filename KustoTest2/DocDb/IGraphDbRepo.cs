//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="IGraphDbRepo.cs" company="Microsoft Corporation">
//   Copyright (C) Microsoft Corporation.  All rights reserved.
//  </copyright>
//  --------------------------------------------------------------------------------------------------------------------

namespace KustoTest2.DocDb
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IGraphDbRepo<V, E> : IDisposable
        where V : class, IGremlinVertex, new()
        where E : class, IGremlinEdge, new()
    {
        Task<IEnumerable<V>> Query(V fromVertex, string query, CancellationToken cancel);
        Task<IEnumerable<E>> GetPath(V fromVertex, V toVertex, CancellationToken cancel);
        Task BulkInsertVertices(IEnumerable<V> vertices, string partition, CancellationToken cancel);
        Task BulkInsertEdges(IEnumerable<E> edges, string partition, CancellationToken cancel);
    }
}