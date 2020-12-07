using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SI4T.Query.Models
{
    public class ElasticSearchResult
    {
        public int Total { get; set; }
        public int Start { get; set; }
        public int PageSize { get; set; }
        public List<SearchResult> Items { get; set; }
        public bool HasError { get; set; }
        public string ErrorDetail { get; set; }
        public string QueryText { get; set; }
        public string QueryUrl { get; set; }
        public List<string> Suggester { get; set; }

        //public List<Facet> Facets { get; set; }

        public ElasticSearchResult()
        {
                Suggester = new List<string>();
                Items = new List<SearchResult>();
        }
    }
}
