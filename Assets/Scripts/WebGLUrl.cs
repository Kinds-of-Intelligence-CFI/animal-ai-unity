using System;

/// <summary>
/// Reads the experiment page's URL, which names the experiment and the participant in its query string.
/// </summary>
public static class WebGLUrl
{
    public const string EXPERIMENT_ID_PARAMETER = "experiment_id";
    public const string PARTICIPANT_ID_PARAMETER = "participant_id";

    /// <summary>
    /// Returns the value of a query parameter in a URL, or an empty string if the URL doesn't have it.
    /// </summary>
    public static string QueryParameter(string url, string name)
    {
        int queryStart = url.IndexOf('?');
        if (queryStart < 0)
        {
            return "";
        }

        string query = url.Substring(queryStart + 1).Split('#')[0];
        foreach (string argument in query.Split('&'))
        {
            int separator = argument.IndexOf('=');
            if (separator >= 0 && argument.Substring(0, separator) == name)
            {
                return Uri.UnescapeDataString(argument.Substring(separator + 1));
            }
        }
        return "";
    }

    /// <summary>
    /// Whether a URL is the tutorial page's, going by its path.
    /// </summary>
    public static bool IsTutorial(string url)
    {
        return url.Split('?')[0].Contains("tutorial");
    }
}
