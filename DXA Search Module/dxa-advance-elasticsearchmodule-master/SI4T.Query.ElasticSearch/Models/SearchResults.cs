using System.Collections.Generic;

namespace SI4T.Query.ElasticSearch.Models
{
    /// <summary>
    /// Extended version of SI4T.Query's <see cref="ElasticSearchResult"/> returned by the CloudSearch Provider.
    /// </summary>
    /// <remarks>
    /// The ElasticSearch Provider uses this class to provide access to the Facets returned by CloudSearch.
    /// Since regular Solr also supports Facets, this facility should be moved up to <see cref="ElasticSearchResult"/>, in which case this class becomes redundant.
    /// </remarks>
    public class SearchResults 
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

        public List<Facet> Facets { get; set; }

        public SearchResults()
        {
            Facets = new List<Facet>();
            Items = new List<SearchResult>();
        }
    }
}
