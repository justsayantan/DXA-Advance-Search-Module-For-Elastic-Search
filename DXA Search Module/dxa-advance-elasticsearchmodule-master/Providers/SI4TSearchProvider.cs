using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Logging;
using Sdl.Web.Common.Models;
using Sdl.Web.Modules.Search.Models;
using SI4T.Query.ElasticSearch.Models;

namespace Sdl.Web.Modules.Search.Providers
{
    /// <summary>
    /// Abstract base class for SI4T-based Search Providers.
    /// </summary>
    public abstract class SI4TSearchProvider : ISearchProvider
    {
        #region ISearchProvider members
        public void ExecuteQuery(SearchQuery searchQuery, Type resultType, Localization localization)
        {
            using (new Tracer(searchQuery, resultType, localization))
            {
                string searchIndexUrl = GetSearchIndexUrl(localization);
                NameValueCollection parameters = SetupParameters(searchQuery, localization);
                SearchResults results = ExecuteQuery(searchIndexUrl, parameters,searchQuery.QueryAgreegationParameters);
                if (results.HasError)
                {
                    Log.Error("Error executing Search Query on URL '{0}': {1}", results.QueryUrl ?? searchIndexUrl, results.ErrorDetail);
                }
                Log.Debug("Search Query '{0}' returned {1} results.", results.QueryText ?? results.QueryUrl, results.Total);

                searchQuery.Total = results.Total;
                searchQuery.HasMore = searchQuery.Start + searchQuery.PageSize <= results.Total;
                searchQuery.CurrentPage = ((searchQuery.Start - 1) / searchQuery.PageSize) + 1;
                searchQuery.Facets = results.Facets;
                if (results.Suggester != null && results.Suggester.Count > 0)
                {
                    searchQuery.SuggestionList = new List<string>();
                    foreach (var item in results.Suggester)
                    {
                        searchQuery.SuggestionList.Add(item);
                    }
                }
                foreach (SearchResult result in results.Items)
                {
                    searchQuery.Results.Add(MapResult(result, resultType, searchQuery.SearchItemView));
                }
            }
        }
        #endregion

        public List<string> ExecuteQueryForAutoComplete(string searchTerm, Localization localization)
        {
            using (new Tracer(searchTerm, localization))
            {
                List<string> results = new List<string>();
                string searchIndexUrl = GetSearchIndexUrl(localization);
                NameValueCollection tridionParameters = new NameValueCollection();
                tridionParameters["q"] = searchTerm;
                tridionParameters["type"] = "auto-complete";
                SearchResults tridion_results = ExecuteQuery(searchIndexUrl, tridionParameters,null);

                if (tridion_results.HasError)
                {
                    Log.Error("Error executing Search Query on URL '{0}': {1}", tridion_results.QueryUrl ?? searchIndexUrl, tridion_results.ErrorDetail);
                }
                if (tridion_results.Suggester != null && tridion_results.Suggester.Count > 0)
                {
                    foreach (var item in tridion_results.Suggester)
                    {
                        results.Add(item.ToString());
                    }
                }
                Log.Debug("Search Query '{0}' returned {1} results.", tridion_results.QueryText ?? tridion_results.QueryUrl, tridion_results.Total);


                return results;
            }
        }

        protected virtual string GetSearchIndexUrl(Localization localization)
        {
            // First try the new search.queryURL setting provided by DXA 1.3 TBBs if the Search Query URL can be obtained from Topology Manager.
            string result = localization.GetConfigValue("search.queryURL");
            if (string.IsNullOrEmpty(result))
            {
                result = localization.GetConfigValue("search." + (localization.IsXpmEnabled ? "staging" : "live") + "IndexConfig");
            }
            return result;
        }

        protected abstract SearchResults ExecuteQuery(string searchIndexUrl, NameValueCollection parameters, Dictionary<string, string> agreegationParameter);

        protected virtual SearchItem MapResult(SearchResult result, Type modelType, string viewName)
        {
            SearchItem searchItem = (SearchItem)Activator.CreateInstance(modelType);
            searchItem.MvcData = new MvcData(viewName);

            searchItem.Title = result.Title;
            searchItem.Url = result.Url;
            searchItem.Summary = result.Summary;
            searchItem.CustomFields = result.CustomFields;

            return searchItem;
        }

        protected virtual NameValueCollection SetupParameters(SearchQuery searchQuery, Localization localization)
        {
            NameValueCollection result = new NameValueCollection(searchQuery.QueryStringParameters);
            result["fq"] = "publicationid:" + localization.Id;
            result["q"] = searchQuery.QueryText;
            result["start"] = searchQuery.Start.ToString(CultureInfo.InvariantCulture);
            result["rows"] = searchQuery.PageSize.ToString(CultureInfo.InvariantCulture);

            return result;
        }
    }
}
