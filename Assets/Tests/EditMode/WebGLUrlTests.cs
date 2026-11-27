using NUnit.Framework;

/// <summary>
/// Tests for WebGLUrl.
/// </summary>
public class WebGLUrlTests
{
    private const string ExperimentUrl = "https://example.com/Build/index.html?experiment_id=my_exp&participant_id=abc123";

    [Test]
    public void QueryParameter_ReturnsEachNamedValue()
    {
        Assert.AreEqual("my_exp", WebGLUrl.QueryParameter(ExperimentUrl, WebGLUrl.EXPERIMENT_ID_PARAMETER));
        Assert.AreEqual("abc123", WebGLUrl.QueryParameter(ExperimentUrl, WebGLUrl.PARTICIPANT_ID_PARAMETER));
    }

    [Test]
    public void QueryParameter_DoesNotDependOnParameterOrder()
    {
        string url = "https://example.com/Build/index.html?participant_id=abc123&other=1&experiment_id=my_exp";
        Assert.AreEqual("my_exp", WebGLUrl.QueryParameter(url, WebGLUrl.EXPERIMENT_ID_PARAMETER));
        Assert.AreEqual("abc123", WebGLUrl.QueryParameter(url, WebGLUrl.PARTICIPANT_ID_PARAMETER));
    }

    [Test]
    public void QueryParameter_DecodesTheValueAndIgnoresTheFragment()
    {
        string url = "https://example.com/Build/index.html?experiment_id=my%2Dexp&participant_id=abc123#top";
        Assert.AreEqual("my-exp", WebGLUrl.QueryParameter(url, WebGLUrl.EXPERIMENT_ID_PARAMETER));
        Assert.AreEqual("abc123", WebGLUrl.QueryParameter(url, WebGLUrl.PARTICIPANT_ID_PARAMETER));
    }

    [TestCase("https://example.com/Build/index.html")]
    [TestCase("https://example.com/Build/index.html?")]
    [TestCase("https://example.com/Build/index.html?my_exp&abc123")]
    [TestCase("https://example.com/Build/index.html?my_experiment_id=my_exp")]
    public void QueryParameter_IsEmptyWhenTheParameterIsMissing(string url)
    {
        Assert.AreEqual("", WebGLUrl.QueryParameter(url, WebGLUrl.EXPERIMENT_ID_PARAMETER));
    }

    [Test]
    public void IsTutorial_GoesByThePathNotTheQuery()
    {
        Assert.IsTrue(WebGLUrl.IsTutorial("https://example.com/tutorial.html?experiment_id=my_exp&participant_id=abc123"));
        Assert.IsFalse(WebGLUrl.IsTutorial("https://example.com/Build/index.html?experiment_id=tutorial_exp&participant_id=abc123"));
    }
}
