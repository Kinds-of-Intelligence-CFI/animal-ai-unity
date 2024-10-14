using System;
using NUnit.Framework;
using ArenasParameters;

[TestFixture]
public class ArenasParametersEventArgsTests
{
    [Test]
    public void Constructor_CreatesInstanceWithEmptyArenaYaml()
    {
        var eventArgs = new ArenasParametersEventArgs();
        Assert.IsNull(eventArgs.arenas_yaml);
    }

    [Test]
    public void ArenaYaml_CanBeSetAndRetrieved()
    {
        var eventArgs = new ArenasParametersEventArgs();
        byte[] testData = new byte[] { 1, 2, 3, 4, 5 };

        eventArgs.arenas_yaml = testData;

        Assert.AreEqual(testData, eventArgs.arenas_yaml);
    }

    [Test]
    public void ArenaYaml_CanBeSetToNull()
    {
        var eventArgs = new ArenasParametersEventArgs();
        eventArgs.arenas_yaml = new byte[] { 1, 2, 3 };

        eventArgs.arenas_yaml = null;

        Assert.IsNull(eventArgs.arenas_yaml);
    }

    [Test]
    public void ArenaYaml_CanBeSetToEmptyArray()
    {
        var eventArgs = new ArenasParametersEventArgs();
        byte[] emptyArray = new byte[0];

        eventArgs.arenas_yaml = emptyArray;

        Assert.AreEqual(emptyArray, eventArgs.arenas_yaml);
        Assert.AreEqual(0, eventArgs.arenas_yaml.Length);
    }

    [Test]
    public void ArenaYaml_CanBeSetMultipleTimes()
    {
        var eventArgs = new ArenasParametersEventArgs();
        byte[] firstData = new byte[] { 1, 2, 3 };
        byte[] secondData = new byte[] { 4, 5, 6 };

        eventArgs.arenas_yaml = firstData;
        Assert.AreEqual(firstData, eventArgs.arenas_yaml);

        eventArgs.arenas_yaml = secondData;
        Assert.AreEqual(secondData, eventArgs.arenas_yaml);
    }
}
