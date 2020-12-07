﻿using System.Collections.Generic;
using System.Collections.Specialized;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Logging;
using Sdl.Web.Modules.Search.Models;
using Sdl.Web.Mvc.Configuration;
using SI4T.Query.ElasticSearch.Models;

namespace Sdl.Web.Modules.Search.Providers
{
    class ElasticSearchProvider : SI4TSearchProvider
    {
        protected override NameValueCollection SetupParameters(SearchQuery searchQuery, Localization localization)
        {
            NameValueCollection result = base.SetupParameters(searchQuery, localization);
            return result;
        }

        protected override SearchResults ExecuteQuery(string searchIndexUrl, NameValueCollection parameters, Dictionary<string, string> agreegationParameter)
        {
            using (new Tracer(searchIndexUrl, parameters))
            {
                string searchUserId = GetSearchUserId(WebRequestContext.Localization);
                string searchPassword = GetSearchPassword(WebRequestContext.Localization);

                SI4T.Query.ElasticSearch.Connection elasticSearchConnection = new SI4T.Query.ElasticSearch.Connection(searchIndexUrl, searchUserId, searchPassword);
                return elasticSearchConnection.ExecuteQuery(parameters, agreegationParameter);
            }
        }

        protected virtual string GetSearchUserId(Localization localization)
        {
            // First try the new search.queryUserId setting provided by DXA TBBs if the Search Query Password can be obtained from Topology Manager.
            string result = localization.GetConfigValue("search.queryUserId");
            if (string.IsNullOrEmpty(result))
            {
                result = localization.GetConfigValue("search." + (localization.IsXpmEnabled ? "staging" : "live") + "IndexUserId");
            }
            return result;
        }

        protected virtual string GetSearchPassword(Localization localization)
        {
            // First try the new search.queryPassword setting provided by DXA TBBs if the Search Query Password can be obtained from Topology Manager.
            string result = localization.GetConfigValue("search.queryPassword");
            if (string.IsNullOrEmpty(result))
            {
                result = localization.GetConfigValue("search." + (localization.IsXpmEnabled ? "staging" : "live") + "IndexPassword");
            }
            return result;
        }
    }
}