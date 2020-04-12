//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="IDocDbRepo.cs" company="Microsoft Corporation">
//   Copyright (C) Microsoft Corporation.  All rights reserved.
//  </copyright>
//  --------------------------------------------------------------------------------------------------------------------

namespace KustoTest2.DocDb
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;

    public interface IDocDbRepo<T>: IDisposable where T : class, new()
    {
        Task<List<T>> QueryAll(CancellationToken cancel);

        Task<List<T>> Query(string whereClause, CancellationToken cancel);

        Task Query(
            SqlQuerySpec querySpec,
            Func<List<T>, CancellationToken, Task> onReceived,
            int batchSize = 1000,
            FeedOptions feedOptions = null,
            CancellationToken cancel = default);

        Task<int> UpsertObjects(List<T> list, CancellationToken cancel = default);
    }
}