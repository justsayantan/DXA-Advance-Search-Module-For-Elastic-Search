using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Sdl.Web.Modules.Search
{
    public static class GenericHelper
    {
        public static string GetValueFromQueryString(string query)
        {
            NameValueCollection queryString = new NameValueCollection(HttpContext.Current.Request.QueryString);
            if (!string.IsNullOrEmpty(queryString[query]))
            {
                return queryString[query];
            }
            return null;
        }

        public static bool CompareInComaSeperatedString(string comaSeperatedString, string backetItem)
        {
            bool result = false;
            List<string> mainString = comaSeperatedString.Split(',').ToList();
            result = mainString.Any(x => x.ToLower() == backetItem.ToLower());
            return result;
        }

        public static string ToLowerFirstChar(string input)
        {
            string newString = input;
            if (!String.IsNullOrEmpty(newString) && Char.IsUpper(newString[0]))
                newString = Char.ToLower(newString[0]) + newString.Substring(1);
            return newString;
        }
    }
}
