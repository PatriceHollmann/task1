using System;

namespace task1
{
    public static class Adresses
    {
        public static string ToAbsoluteUrl(string relativeUri, string baseUrl)
        {
            if (relativeUri.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || relativeUri.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return relativeUri;

            if (relativeUri.StartsWith("//www", StringComparison.OrdinalIgnoreCase))
            {
                var prefix = baseUrl.Split('/');
                relativeUri = relativeUri.Insert(0, prefix[0]);
                return relativeUri;
            }

            if (relativeUri.StartsWith("../", StringComparison.OrdinalIgnoreCase))
            {
                Uri baseURL = new Uri(baseUrl);
                Uri relativeURL = new Uri(relativeUri.TrimStart(new char[]{'.','/' }));
                Uri currentUrl = new Uri(baseURL, relativeURL);
                return currentUrl.ToString();
            }
            else
            {
                Uri baseURL = new Uri(baseUrl);
                Uri relativeURL = new Uri(relativeUri);
                Uri currentUrl = new Uri(baseURL, relativeURL);
                return currentUrl.ToString();
            }
        }
    }

}
