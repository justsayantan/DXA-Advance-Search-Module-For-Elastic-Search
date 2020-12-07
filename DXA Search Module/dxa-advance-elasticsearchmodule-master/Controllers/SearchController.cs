using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web.Mvc;
using Sdl.Web.Common.Logging;
using Sdl.Web.Common.Models;
using Sdl.Web.Modules.Search.Models;
using Sdl.Web.Modules.Search.Providers;
using Sdl.Web.Mvc.Configuration;
using Sdl.Web.Mvc.Controllers;

namespace Sdl.Web.Modules.Search.Controllers
{
    public class SearchController : EntityController
    {
        protected ISearchProvider SearchProvider { get; set; }
        private const string LabelForYes = "yes";
        private const string LabelForCategoryType = "512";

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="searchProvider">The Search Provider to use. Set using Dependency Injection (ISearchProvider interface must be defined in Unity.config)</param>
        public SearchController(ISearchProvider searchProvider)
        {
            SearchProvider = searchProvider;
        }

        [Route("~/Search/GetAutoCompleteSearchReasult")]
        public JsonResult GetAutoCompleteSearchReasult(string term)
        {
            if (WebRequestContext.Localization.GetConfigValue("search.isAutoCompleteEnable").ToLower() == LabelForYes)
            {
                List<string> autoCompleteSuggestion = new List<string>();
                var result = SearchProvider.ExecuteQueryForAutoComplete(term, WebRequestContext.Localization);
                autoCompleteSuggestion = result.Distinct().ToList();
                return Json(autoCompleteSuggestion, JsonRequestBehavior.AllowGet);
            }
            else
            {
                Log.Info("Auto Complete Feature is not enabled");
                return null;
            }
        }


        /// <summary>
        /// Enrich the SearchQuery View Model with request querystring parameters and populate the results using a configured Search Provider.
        /// </summary>
        protected override ViewModel EnrichModel(ViewModel model)
        {
            using (new Tracer(model))
            {
                base.EnrichModel(model);

                SearchQuery searchQuery = model as SearchQuery;
                if (searchQuery == null || !searchQuery.GetType().IsGenericType)
                {
                    throw new DxaSearchException(String.Format("Unexpected View Model: '{0}'. Expecting type SearchQuery<T>.", model));
                }

                NameValueCollection queryString = Request.QueryString;
                // Map standard query string parameters
                searchQuery.QueryText = queryString["q"];
                searchQuery.Start = queryString.AllKeys.Contains("start") ? Convert.ToInt32(queryString["start"]) : 1;
                // To allow the Search Provider to use additional query string parameters:
                searchQuery.QueryStringParameters = queryString;

                //Search Query Aggregation Paremeters for Faceted Search
                string isFacetedSearchEnable = WebRequestContext.Localization.GetConfigValue("search.isFacetedSearchEnable");
                if (!string.IsNullOrEmpty(isFacetedSearchEnable) && isFacetedSearchEnable.ToLower() == LabelForYes)
                {
                    searchQuery.QueryAgreegationParameters = GetAgreegationParameters();
                }
                Type searchItemType = searchQuery.GetType().GetGenericArguments()[0];
                SearchProvider.ExecuteQuery(searchQuery, searchItemType, WebRequestContext.Localization);

                string isSuggesterEnable = WebRequestContext.Localization.GetConfigValue("search.isSuggesterEnable");
                
                if (!string.IsNullOrEmpty(isSuggesterEnable) && isSuggesterEnable.ToLower()== LabelForYes)
                {
                    if (searchQuery.SuggestionList != null && searchQuery.SuggestionList.Count > 0)
                    {
                        searchQuery.SuggestionText = string.Join(", ", searchQuery.SuggestionList);
                        searchQuery.SuggestionText = string.Format(WebRequestContext.Localization.GetConfigValue("search.suggestionResultText"), searchQuery.SuggestionText);
                    }
                }
                return searchQuery;
            }
        }

        /// <summary>
        /// Get all the parameters from catregory id
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, string> GetAgreegationParameters()
        {
            Dictionary<string, string> keyValuePairs = new Dictionary<string, string>();
            string categoryIdOfFacetedSearch = WebRequestContext.Localization.GetConfigValue("search.idOfFacetedSearchCategory");
            if (!string.IsNullOrEmpty(categoryIdOfFacetedSearch))
            {
                List<string> categoryNames = new List<string>();
                if (!string.IsNullOrEmpty(categoryIdOfFacetedSearch))
                {                    
                    string id = $"tcm:{WebRequestContext.Localization.Id}-{categoryIdOfFacetedSearch}-{LabelForCategoryType}";
                    keyValuePairs = ContentDeliveryProvider.GetKeywordDetailFromCategory(id);                    
                }
            }

            return keyValuePairs;
        }
    }
}