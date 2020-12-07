using System;
using System.Collections.Generic;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Modules.Search.Models;

namespace Sdl.Web.Modules.Search.Providers
{
    public interface ISearchProvider
    {
        void ExecuteQuery(SearchQuery searchQuery, Type resultType, Localization localization);

        List<string> ExecuteQueryForAutoComplete(string searchTerm, Localization localization);
    }
}
