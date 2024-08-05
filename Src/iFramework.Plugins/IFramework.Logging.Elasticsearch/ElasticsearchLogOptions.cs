using System;
using System.Collections.Generic;
using System.Text;
using Elastic.Extensions.Logging;
using Elastic.Ingest.Elasticsearch.Indices;

namespace IFramework.Logging.Elasticsearch
{
    public class ElasticsearchLogOptions
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string BaseAddress { get; set; }
        public string App { get; set; }
        public string Index { get; set; }
        public string Env { get; set; }
        public Action<IndexChannelOptions<LogEvent>> IndexChannelOptionsAction { get; set; }
    }
}
