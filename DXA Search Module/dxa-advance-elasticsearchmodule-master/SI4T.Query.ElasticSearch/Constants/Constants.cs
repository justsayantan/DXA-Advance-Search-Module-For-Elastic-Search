namespace SI4T.Query.ElasticSearch
{
    //TODO: Need to remove all the constant and bring it from resource component
    public class Constants
    {
        public static string ContentGroupNameIdDelimiter = "-id-";

        public static string PrequeryFilterFieldNameForSites = "contentGroup";
        public static string PrequeryFilterFieldNameForDocs = "dynamic.FWFCONTENTGROUP#logical#value";
        public static string DocsChannelFieldName = "FWFCHANNEL#version#value";
        public static string EffectiveDateSearchResultFieldName = "wfEffectiveDate";
        public static string TopicUrl = "publication";
        public static string BinaryUrl = "binary";
        public static string[] Fields = new []{ "body", "publicationTitle", "rawContent" ,  "title" };
    }
}