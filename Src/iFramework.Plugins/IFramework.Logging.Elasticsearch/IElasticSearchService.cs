using Refit;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IFramework.Logging.Elasticsearch
{
    public interface IElasticSearchService
    {
        [Put("/")]
        Task<string> AddLog([Body]IEnumerable<IDictionary<string, object>> request);
    }
}

