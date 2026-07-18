namespace eQuantic.Linq.Web.Syntax;

/// <summary>Minimal, framework-free query-string splitter with URL decoding.</summary>
internal static class QueryStringSplitter
{
    public static List<KeyValuePair<string, string>> Split(string queryString)
    {
        var pairs = new List<KeyValuePair<string, string>>();

        if (string.IsNullOrWhiteSpace(queryString))
        {
            return pairs;
        }

        var text = queryString.Trim();
        if (text.StartsWith("?", StringComparison.Ordinal))
        {
            text = text.Substring(1);
        }

        foreach (var segment in text.Split('&'))
        {
            if (segment.Length == 0)
            {
                continue;
            }

            var separator = segment.IndexOf('=');
            var key = separator < 0 ? segment : segment.Substring(0, separator);
            var value = separator < 0 ? string.Empty : segment.Substring(separator + 1);

            pairs.Add(new KeyValuePair<string, string>(Decode(key), Decode(value)));
        }

        return pairs;
    }

    private static string Decode(string value) =>
        Uri.UnescapeDataString(value.Replace('+', ' '));
}
