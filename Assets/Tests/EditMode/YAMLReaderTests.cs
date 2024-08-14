using NUnit.Framework;
using UnityEngine;
using YAMLDefs;
using System.Collections.Generic;

/// <summary>
/// Tests for the YAMLReader class.
/// Currently only tests the Setup method.
/// </summary>
public class YAMLReaderTests
{
    private YAMLReader yamlReader;

    [SetUp]
    public void Setup()
    {
        yamlReader = new YAMLReader();
    }
}
