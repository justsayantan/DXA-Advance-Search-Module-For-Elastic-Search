using Nest;
using Newtonsoft.Json;
using SI4T.Query.ElasticSearch.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;

namespace SI4T.Query.ElasticSearch
{
    public class Connection
    {
        #region Variable
        /// <summary>
        /// URL of the ElasticSearch Search endpoint
        /// </summary>
        public string ServiceUrl { get; set; }

        public string UserId { get; set; }

        public string Password { get; set; }

        /// <summary>
        /// Number of characters for auto-generated summary data
        /// </summary>
        public int AutoSummarySize { get; set; }

        /// <summary>
        /// Default page size
        /// </summary>
        public int DefaultPageSize { get; set; }

        /// <summary>
        /// Maximum number of Facets to return
        /// </summary>
        public int MaxNumberOfFacets { get; set; }

        /// <summary>
        /// Search Term
        /// </summary>
        private string SearchTerm { get; set; }

        private string[] searchFields = new[] { "body", "summary", "title" };
        //private string[] searchFields = new[] { "publicationTitle"};
        #endregion

        #region Public Method
        public Connection(string serviceUrl, string userId = null, string password = null)
        {
            ServiceUrl = serviceUrl;
            UserId = userId;
            Password = password;
            AutoSummarySize = 255;
            DefaultPageSize = 10;
            MaxNumberOfFacets = 100;
        }

        /// <summary>
        /// Run a query
        /// </summary>
        /// <param name="parameters">The query parameters</param>
        /// <returns>matching results</returns>
        public SearchResults ExecuteQuery(NameValueCollection parameters, Dictionary<string,string>  agreegationParameter)
        {
            SearchResults result = new SearchResults();

            ElasticClient client = GetElasticSearchClient();

            if(parameters["type"] == "auto-complete")
            {
                SearchRequest request = BuildAutoCompleteSearchRequest(parameters, client.ConnectionSettings.DefaultIndex);
                ISearchResponse<object> response = client.Search<object>(request);

                result.Items = response.Hits.Select(hit => CreateSearchResult(hit)).ToList();
                result.Suggester = GetSuggesterValueFromSearchResponse(response, parameters["q"]);
                result.QueryText = parameters["q"];
            }
            else
            {
                // General case for Sites CMS.
                SearchRequest request = BuildSearchRequest(parameters,agreegationParameter, client.ConnectionSettings.DefaultIndex);
                var response = client.Search<object>(request);
                result.Items = response.Hits.Select(hit => CreateSearchResult(hit)).ToList();
                result.Suggester = GetSuggesterValueFromSearchResponse(response, parameters["q"]);
                result.Facets = GetAggregationValueFromSearchResponse(response);
                result.PageSize = Convert.ToInt32(request.Size);
                result.Total = Convert.ToInt32(response.Total);
                result.Start = Convert.ToInt32(request.From);
                result.QueryText = parameters["q"];
            }
            
            return result;
        }



        #endregion

        #region Private Method
        private List<Facet> GetAggregationValueFromSearchResponse(ISearchResponse<object> response)
        {
            List<Facet> facets = new List<Facet>();
            if(response.Aggregations != null && response.Aggregations.Count > 0)
            {
                foreach (var key in response.Aggregations.Keys)
                {
                    facets.Add(CreatFacetsFromAggregation(response.Aggregations.Terms(key).Buckets, key));
                }
            }
            return facets;
        }

        /// <summary>
        /// This Method helps to generate facet from Aggregation. 
        /// </summary>
        /// <param name="buckets"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        private Facet CreatFacetsFromAggregation(IReadOnlyCollection<KeyedBucket<string>> buckets, string key)
        {
            Facet facet = new Facet();
            if (buckets.Count > 0)
            {
                List<Models.Bucket> bucketList = new List<Models.Bucket>();                             
                foreach (var item in buckets)
                {
                    Models.Bucket bucket = new Models.Bucket(item.Key, item.DocCount.Value);
                    bucketList.Add(bucket);                    
                }

                facet.Name = key;
                facet.Buckets = bucketList;
            }
            return facet;
        }

        /// <summary>
        /// This Method returns the List of Suggestion based on Term or Phrase
        /// </summary>
        /// <param name="response"></param>
        /// <param name="searchTerm"></param>
        /// <returns></returns>
        private List<string> GetSuggesterValueFromSearchResponse(ISearchResponse<object> response, string searchTerm)
        {
            List<string> suggester = new List<string>();
            if (!string.IsNullOrEmpty(searchTerm) && searchTerm.Split(' ').Length == 1)
            {
                suggester = (from res in response.Suggest.Values
                             from val in res
                             from option in val.Options
                             select option.Text).ToList();
            }
            else if (!string.IsNullOrEmpty(searchTerm) && searchTerm.Split(' ').Length > 1)
            {
                suggester = (from res in response.Suggest.Values
                                from val in res
                                from option in val.Options
                                where option.CollateMatch == true
                                select option.Highlighted).ToList();
            }
            return suggester.Distinct().ToList();
        }

        /// <summary>
        /// This Methods prepare the search request.
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="indexName"></param>
        /// <returns></returns>
        private SearchRequest BuildSearchRequest(NameValueCollection parameters, Dictionary<string, string> agreegationParameter, string indexName)
        {
            string start = parameters["start"] ?? "1";
            string rows = parameters["rows"] ?? DefaultPageSize.ToString(CultureInfo.InvariantCulture);
            
            List<QueryContainer> mustClauses = new List<QueryContainer>();
            List<QueryContainer> shouldClauses = new List<QueryContainer>();
            List<QueryContainer> filterClauses = new List<QueryContainer>();

            Field field1 = new Field("publicationTitle",null,null);
            if (parameters["q"] != null)
            {
                mustClauses.Add(new QueryStringQuery
                {
                    Fields = searchFields,
                    Query = parameters["q"],
                    AnalyzeWildcard = false,
                    Lenient = false
                });
            }

            if (!string.IsNullOrEmpty(parameters["fq"]))
            {
                string[] filterQueryParameters = parameters["fq"].Split(new string[] { "||" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string item in filterQueryParameters)
                {
                    string[] keyValue = item.Split(':');
                    {
                        if (keyValue.Length > 0)
                        {
                            if (keyValue[0] == "date")
                            {
                                string[] range = keyValue[1].Split(',');
                                if (range.Length > 0)
                                {
                                    filterClauses.Add(new DateRangeQuery
                                    {
                                        Field = new Field("date"),
                                        GreaterThanOrEqualTo = DateTime.ParseExact(range[0], "yyyy-MM-dd", CultureInfo.InvariantCulture),
                                        LessThanOrEqualTo = DateTime.ParseExact(range[1], "yyyy-MM-dd", CultureInfo.InvariantCulture)
                                    });
                                }
                                else
                                {
                                    filterClauses.Add(new TermQuery
                                    {
                                        Field = new Field("date"),
                                        Value = keyValue[1]
                                    });
                                }
                            }
                            else
                            {
                                filterClauses.Add(new QueryStringQuery
                                {
                                    Fields = new Field[] { (keyValue[0]) },
                                    Query = keyValue[1].Contains(",") ? keyValue[1].Replace(",", " OR ") : keyValue[1]
                                });
                            }
                        }
                    }

                }
            }

            //#region Pre Query Filter
            //string prequeryFilterFieldNameForSites = "";// GenericHelpers.GetContentGroupFieldName("sites");
            //if (!string.IsNullOrEmpty(parameters["wfprequeryfilter"]) && parameters["wfprequeryfilter"].ToString() != "Search All")
            //{
            //    var prequeryField = new Field(prequeryFilterFieldNameForSites);
            //    Field[] prequeryFieldAsArray = { prequeryField };
            //    filterClauses.Add(new QueryStringQuery
            //    {
            //        Fields = prequeryFieldAsArray,
            //        // Enclose in double quotes to ensure that the term, even with spaces, is treated as a single query term.
            //        Query = "\"" + parameters["wfprequeryfilter"] + "\""
            //    });
            //}
            //#endregion

            #region Post Query Filter 
            if (agreegationParameter != null && agreegationParameter.Any())
            {
                foreach (var item in agreegationParameter)
                {
                    if (!string.IsNullOrEmpty(parameters[item.Key]))
                    {
                        List<string> values = parameters[item.Key].Split(',').ToList();
                        string _query = "\"" + string.Join("\", \"", values) + "\"";
                        string fieldName = $"{item.Value}.keyword"; 
                        filterClauses.Add(new QueryStringQuery
                        {
                            Fields = new Field[] { fieldName },
                            Query = _query.Contains(",") ? _query.Replace(",", " OR ") : _query
                        });
                    }
                }
            }                
            #endregion

            #region Highlight
            Highlight highlight = new Highlight
            {
                Order = HighlighterOrder.Score,
                PreTags = new[] { "<b style=\"background-color:#FFFF00\"><i>" },
                PostTags = new[] { "</i></b>" },
                Fields = new Dictionary<Field, IHighlightField>
                {
                    {
                        "summary", new HighlightField
                        {
                            Type = HighlighterType.Plain,
                            ForceSource = true,
                            FragmentSize = 150,
                            Fragmenter = HighlighterFragmenter.Span,
                            NumberOfFragments = 3,
                            NoMatchSize = 150,
                        }
                    },
                    { "body", new HighlightField
                        {
                            Type = HighlighterType.Plain,
                            ForceSource = true,
                            FragmentSize = 150,
                            Fragmenter = HighlighterFragmenter.Span,
                            NumberOfFragments = 3,
                            NoMatchSize = 150,
                        }
                    },
                    {
                        "title", new HighlightField
                        {
                            Type = HighlighterType.Plain,
                            ForceSource = true,
                            FragmentSize = 150,
                            Fragmenter = HighlighterFragmenter.Span,
                            NumberOfFragments = 3,
                            NoMatchSize = 150,
                        }
                    }
                }
            };
            #endregion

            #region Suggester
            PhraseSuggestHighlight phraseSuggestHighlight = new PhraseSuggestHighlight
            {
                PreTag = "<b style=\"background-color:#FFFF00\"><i>",
                PostTag = "</i></b>"
            };
            SuggestContainer suggest = null;
            if (!string.IsNullOrEmpty(parameters["q"]) && parameters["q"].Split(' ').Length == 1)
            {
                suggest = new SuggestContainer
                {
                    {
                        "my-term-suggest-from-field", new SuggestBucket
                        {
                            Text = parameters["q"],
                            Term = new TermSuggester
                            {
                                Field = "title"
                            }
                        }
                    },
                    {
                        "my-term-suggest-from-body", new SuggestBucket
                        {
                            Text = parameters["q"],
                            Term = new TermSuggester
                            {
                                Field = "body"
                            }
                        }
                    }
                };
            }
            else if (!string.IsNullOrEmpty(parameters["q"]) && parameters["q"].Split(' ').Length > 1)
            {
                suggest = new SuggestContainer
                {
                    {
                        "my-phrase-suggest-from-field", new SuggestBucket
                        {
                            Text = parameters["q"],
                            Phrase = new PhraseSuggester
                            {
                                Field = "title",
                                RealWordErrorLikelihood = 0.95,
                                MaxErrors = 0.5,
                                Confidence = 0,
                                Highlight = phraseSuggestHighlight,
                                GramSize = 1,
                                TokenLimit = 5,
                                ForceUnigrams = false,
                                Collate = new PhraseSuggestCollate
                                {

                                    Query = new PhraseSuggestCollateQuery
                                    {
                                        Source = "{ \"match\": { \"{{field_name}}\": \"{{suggestion}}\" }}",
                                    },
                                    Params = new Dictionary<string, object>
                                    {
                                        { "field_name", "title" }
                                    },
                                    Prune = true
                                },
                            }
                        }
                    }
                    ,
                    {
                        "my-phrase-suggest-from-body", new SuggestBucket
                        {
                            Text = parameters["q"],
                            Phrase = new PhraseSuggester
                            {
                                Field = "body",
                                RealWordErrorLikelihood = 0.95,
                                MaxErrors = 0.5,
                                Confidence = 0,
                                Highlight = phraseSuggestHighlight,
                                GramSize = 1,
                                TokenLimit = 5,
                                ForceUnigrams = false,
                                Collate = new PhraseSuggestCollate
                                {

                                    Query = new PhraseSuggestCollateQuery
                                    {
                                        Source = "{ \"match\": { \"{{field_name}}\": \"{{suggestion}}\" }}",
                                    },
                                    Params = new Dictionary<string, object>
                                    {
                                        { "field_name", "body" }
                                    },
                                    Prune = true
                                },
                            }
                        }
                    }
                };
            }
            #endregion

            #region Aggregation
            Dictionary<string, IAggregationContainer> aggregations = new Dictionary<string, IAggregationContainer>();
            if (agreegationParameter != null && agreegationParameter.Any())
            {                
                foreach (var item in agreegationParameter)
                {
                    string fieldName = $"{item.Value}.keyword";
                    aggregations.Add(item.Key, new AggregationContainer
                    {
                        Terms = new TermsAggregation(item.Key)
                        {
                            Field = fieldName,
                            ExecutionHint = TermsAggregationExecutionHint.GlobalOrdinals
                        }
                    });
                }                    
            }
            #endregion

            #region Sort
            List<ISort> sort = new List<ISort>();
            if (!string.IsNullOrEmpty(parameters["sort"]))
            {
                string[] sortQueryParameter = parameters["sort"].Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);

                if (sortQueryParameter.Length > 0)
                {
                    sort.Add(new FieldSort() { Field = sortQueryParameter[0], Order = sortQueryParameter[1] == "desc" ? SortOrder.Descending : SortOrder.Ascending });
                }
            }
            #endregion

            QueryContainer query = new BoolQuery
            {
                Must = mustClauses,
                Should = shouldClauses,
                Filter = filterClauses
            };

            return new SearchRequest(indexName)
            {
                From = Convert.ToInt32(start) - 1, // SI4T uses 1 based indexing, but CloudSearch and Elasticsearch uses 0 based.
                Size = Convert.ToInt32(rows),
                Query = query,
                Highlight = highlight,
                Sort = sort,
                Suggest = suggest,
                Aggregations = aggregations
            };
        }
        
        /// <summary>
        /// Build Search Request For AutoComplete feature
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="indexName"></param>
        /// <returns></returns>
        private SearchRequest BuildAutoCompleteSearchRequest(NameValueCollection parameters, string indexName)
        {
            string start = parameters["start"] ?? "1";
            string rows = parameters["rows"] ?? DefaultPageSize.ToString(CultureInfo.InvariantCulture);

            List<QueryContainer> mustClauses = new List<QueryContainer>();
            List<QueryContainer> filterClauses = new List<QueryContainer>();

            if (parameters["q"] != null)
            {
                mustClauses.Add(new QueryStringQuery
                {
                    Fields = searchFields,
                    Query = parameters["q"],
                    AnalyzeWildcard = false,
                    Lenient = false
                });
            }

            if (!string.IsNullOrEmpty(parameters["fq"]))
            {
                string[] filterQueryParameters = parameters["fq"].Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string item in filterQueryParameters)
                {
                    string[] keyValue = item.Split(':');
                    {
                        if (keyValue.Length > 0)
                        {
                            if (keyValue[0] == "date")
                            {
                                string[] range = keyValue[1].Split(',');
                                if (range.Length > 0)
                                {
                                    filterClauses.Add(new DateRangeQuery
                                    {
                                        Field = new Field("date"),
                                        GreaterThanOrEqualTo = DateTime.ParseExact(range[0], "yyyy-MM-dd", CultureInfo.InvariantCulture),
                                        LessThanOrEqualTo = DateTime.ParseExact(range[1], "yyyy-MM-dd", CultureInfo.InvariantCulture)
                                    });
                                }
                                else
                                {
                                    filterClauses.Add(new TermQuery
                                    {
                                        Field = new Field("date"),
                                        Value = keyValue[1]
                                    });
                                }
                            }
                            else
                            {
                                mustClauses.Add(new TermQuery
                                {
                                    Field = new Field(keyValue[0]),
                                    Value = keyValue[1]
                                });
                            }
                        }
                    }

                }
            }


            QueryContainer query = new BoolQuery
            {
                Must = mustClauses,
                Filter = filterClauses
            };

            Highlight highlight = new Highlight
            {
                Order = HighlighterOrder.Score,
                Fields = new Dictionary<Field, IHighlightField>
                {
                    { "body", new HighlightField
                        {
                            Type = HighlighterType.Plain,
                            ForceSource = true,
                            FragmentSize = 150,
                            Fragmenter = HighlighterFragmenter.Span,
                            NumberOfFragments = 3,
                            NoMatchSize = 150
                        }
                    }
                }
            };

            #region Suggester
            SuggestContainer suggest = new SuggestContainer
            {
                {
                    "my-completion-suggest", new SuggestBucket
                    {
                        Prefix = parameters["q"],
                        Completion = new CompletionSuggester
                        {
                            Field = "title.completion",
                            Fuzzy = new SuggestFuzziness()
                            {
                                Fuzziness = Fuzziness.Auto,
                                PrefixLength = 3,
                                Transpositions = true,
                                UnicodeAware = false
                            },
                            Analyzer = "simple"
                        }
                    }
                },
                {
                    "my-term-suggest-from-publicationTitle", new SuggestBucket
                    {
                        Text = parameters["q"],
                        Completion = new CompletionSuggester
                        {
                            Field = "body.completion",
                            Fuzzy = new SuggestFuzziness()
                            {
                                Fuzziness = Fuzziness.Auto,
                                PrefixLength = 3,
                                Transpositions = true,
                                UnicodeAware = false
                            },
                            Analyzer = "simple"
                        }
                    }
                }
            };


            #endregion


            return new SearchRequest(indexName)
            {
                From = Convert.ToInt32(start) - 1, // SI4T uses 1 based indexing, but CloudSearch and Elasticsearch uses 0 based.
                Size = Convert.ToInt32(rows),
                Query = query,
                Suggest = suggest
            };
        }

        private ElasticClient GetElasticSearchClient()
        {
            
            string IndexName = string.Empty;
            string ElasticInstanceUri = string.Empty;

            if (!string.IsNullOrEmpty(ServiceUrl))
            {
                IndexName = ServiceUrl.Split('/').Last();
                ElasticInstanceUri = ServiceUrl.Substring(0, ServiceUrl.LastIndexOf("/"));
            }

            Uri node = new Uri(ElasticInstanceUri);
            ConnectionSettings settings = new ConnectionSettings(node);

            //var settings = new ConnectionSettings(node)
            //    .DefaultMappingFor<SearchResult>(m => m
            //        .IndexName(IndexName)
            //    );
            if (UserId != null && Password != null)
            {
                settings.BasicAuthentication(UserId, Password);
            }
            settings.DefaultIndex(IndexName);
            return new ElasticClient(settings);
        }
      

        private SearchResult CreateSearchResult(IHit<object> hit)
        {
            SearchResult result = new SearchResult { Id = hit.Id };
            string resultjson = hit.Source.ToString();
            Dictionary<string, object> fields = (Dictionary<string, object>)hit.Source;
            //Dictionary<string, object> fields1 = JsonConvert.DeserializeObject<Dictionary<string, object>>(hit.Source.ToString());

            foreach (KeyValuePair<string, object> field in fields)
            {
                string type = field.Value.GetType().ToString();
                string fieldname = field.Key;

                switch (fieldname.ToLower())
                {
                    case "publicationid":
                        result.PublicationId = int.Parse(field.Value.ToString());
                        break;
                    case "title":
                        result.Title = field.Value.ToString();
                        break;
                    case "publicationtitle":
                        result.PublicationTitle = field.Value.ToString();
                        break;
                    case "topictitle":
                        result.TopicTitle = field.Value.ToString();
                        break;
                    case "url":
                        result.Url = field.Value.ToString();
                        break;
                    case "summary":
                        result.Summary = field.Value.ToString();
                        break;                    
                    case "rawcontent":
                        result.RawContent = field.Value.ToString();
                        break;
                    case "contentgroup":
                        result.ContentGroups = ConvertStringToArray(field.Value);
                        break;
                    case "channel":
                       //  = (field.Value);
                       result.Channels = ConvertStringToArray(field.Value);
                        break;
                    case "type":
                        result.Type = field.Value.ToString();
                        break;
                    default:
                        object data = null;
                        switch (type)
                        {
                            case "arr": //TODO: Make smarter
                                data = field.Value;
                                break;
                            default:
                                data = field.Value.ToString();
                                break;
                        }
                        result.CustomFields.Add(fieldname, data);
                        // Add dynamic fields individually to result.
                        if("dynamic" == fieldname)
                        {
                            Dictionary<string, object> dynamicFields = JsonConvert.DeserializeObject<Dictionary<string, object>>((string)data);
                            // Strip off the "dynamic." prefix.
                            var prequeryFieldName = Constants.PrequeryFilterFieldNameForDocs.Remove(0, 8);
                            if (dynamicFields.ContainsKey(prequeryFieldName))
                            {
                                result.ContentGroup = (string)dynamicFields[prequeryFieldName];
                            }
                             if (dynamicFields.ContainsKey(Constants.DocsChannelFieldName) && (null != dynamicFields[Constants.DocsChannelFieldName]))
                            {
                                result.Channels = ((Newtonsoft.Json.Linq.JArray)dynamicFields[Constants.DocsChannelFieldName]).ToObject<string[]>();
                            }
                        }
                        break;
                }
            }

            if (string.IsNullOrEmpty(result.Summary) && hit.Highlight.ContainsKey("body"))
            {
                // If no summary field is present in the index, use the highlight fragment from the body field instead.
                string autoSummary = hit.Highlight["body"].FirstOrDefault();
                if (autoSummary.Length > AutoSummarySize)
                {
                    // To limit the size of the fragment in the Search Request.
                    // Therefore we truncate it here if needed.
                    autoSummary = autoSummary.Substring(0, AutoSummarySize) + "...";
                }
                result.Summary = autoSummary;
            }
            else if(hit.Highlight.ContainsKey("summary"))
            {
                string summary = hit.Highlight["summary"].FirstOrDefault();
                if (!string.IsNullOrEmpty(summary))
                {
                    result.Summary = summary;
                }
            }

            if (hit.Highlight.ContainsKey("title"))
            {
                // If no summary field is present in the index, use the highlight fragment from the body field instead.
                string title = hit.Highlight["title"].FirstOrDefault();
                if (!string.IsNullOrEmpty(title))
                {
                    result.Title = title;
                }
            }
            return result;
        }

        private string[] ConvertStringToArray(object value)
        {
            string[] values;
            if (value is List<object>)
            {
                values = ((IEnumerable) value).Cast<string>()
                    .Select(x => x == null ? x : x.ToString())
                    .ToArray();
            }
            else
            {
                values =  new string[]{value.ToString()};
            }

            return values;
        }

        #endregion
    }
}
