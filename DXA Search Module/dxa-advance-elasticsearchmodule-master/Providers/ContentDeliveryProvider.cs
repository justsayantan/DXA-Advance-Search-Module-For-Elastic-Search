using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tridion.ContentDelivery.Taxonomies;

namespace Sdl.Web.Modules.Search.Providers
{
    public static class ContentDeliveryProvider
    {
        private const string LableForSearchFieldName = "searchFieldName";
        /// <summary>
        /// This method helps to retrive the Category Detials
        /// </summary>
        /// <param name="category">Category Tcm Id</param>
        /// <returns>Category Name</returns>
        public static string GetCategoryName(string category)
        {
            try
            {
                var factory = new TaxonomyFactory();
                var keyword = factory.GetTaxonomyKeyword(category);
                return keyword.KeywordName;

            }
            catch (Exception ex)
            {
                return null;
            }
        }

        /// <summary>
        /// This method helps to retrive the Category Detials
        /// </summary>
        /// <param name="key">Category Tcm Id</param>
        /// <returns>Category Description</returns>
        public static List<string> GetKeywordTitleFromCategory(string categoryId)
        {
            try
            {
                List<string> keywordTitleList = new List<string>();
                var factory = new TaxonomyFactory();
                var keywords = factory.GetTaxonomyKeywords(categoryId);
                foreach (Keyword childKeyword in keywords.KeywordChildren)
                {
                    keywordTitleList.Add(childKeyword.KeywordName);
                }

                return keywordTitleList;

            }
            catch
            {
                return null;
            }
        }

        internal static Dictionary<string, string> GetKeywordDetailFromCategory(string id)
        {
            try
            {
                Dictionary<string, string> keywordTitleList = new Dictionary<string, string>();
                var factory = new TaxonomyFactory();
                var keywords = factory.GetTaxonomyKeywords(id);
                foreach (Keyword childKeyword in keywords.KeywordChildren)
                {
                    string KeyMetaValue  = (childKeyword.KeywordMeta != null && childKeyword.KeywordMeta.NameValues.Contains(LableForSearchFieldName)) ?  childKeyword.KeywordMeta.GetValue(LableForSearchFieldName).ToString() : GenericHelper.ToLowerFirstChar(childKeyword.KeywordName.Replace(" ", string.Empty));
                    keywordTitleList.Add(childKeyword.KeywordName, KeyMetaValue);
                }

                return keywordTitleList;

            }
            catch
            {
                return null;
            }
        }
    }
}
