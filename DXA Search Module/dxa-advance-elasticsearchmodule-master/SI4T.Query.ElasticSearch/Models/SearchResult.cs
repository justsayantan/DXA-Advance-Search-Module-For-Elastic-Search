using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SI4T.Query.ElasticSearch.Models
{
    public class SearchResult
    {
        public string Id { get; set; }
        public int PublicationId { get; set; }
        public string Url { get; set; }
        public string Title { get; set; }
        public string TopicTitle { get; set; }
        public string PublicationTitle { get; set; }
        public string ContentGroup { get; set; }
        public string[] ContentGroups { get; set; }
        public string Channel { get; set; }
        public string[] Channels { get; set; }
        public string Summary { get; set; }
        public string RawContent { get; set; }
        public string Type { get; set; }
        public Dictionary<string, object> CustomFields { get; set; }
        public SearchResult()
        {
            CustomFields = new Dictionary<string, object>();
        }
    }
}
