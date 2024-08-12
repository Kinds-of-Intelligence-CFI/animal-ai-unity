using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Holders;

/// <summary>
/// Tests for the Holders class.
/// </summary>
public class ListOfBlackScreensTests
{
    [Test]
    public void GetFades_ShouldReturnEmptyList_WhenAllBlackScreensIsNull()
    {
        var listOfBlackScreens = new ListOfBlackScreens { allBlackScreens = null };

        var fades = listOfBlackScreens.GetFades();

        Assert.IsNotNull(fades);
        Assert.AreEqual(0, fades.Count);
    }

    [Test]
    public void GetFades_ShouldReturnEmptyList_WhenAllBlackScreensIsEmpty()
    {
        var listOfBlackScreens = new ListOfBlackScreens
        {
            allBlackScreens = new List<GameObject>()
        };

        var fades = listOfBlackScreens.GetFades();

        Assert.IsNotNull(fades);
        Assert.AreEqual(0, fades.Count);
    }

    [Test]
    public void GetFades_ShouldReturnOnlyNonNullFadeComponents()
    {
        var fadeObject1 = new GameObject();
        fadeObject1.AddComponent<Fade>();

        var fadeObject2 = new GameObject();

        var fadeObject3 = new GameObject();
        fadeObject3.AddComponent<Fade>();

        var listOfBlackScreens = new ListOfBlackScreens
        {
            allBlackScreens = new List<GameObject> { fadeObject1, fadeObject2, fadeObject3 }
        };

        var fades = listOfBlackScreens.GetFades();

        Assert.AreEqual(2, fades.Count);
        Assert.Contains(fadeObject1.GetComponent<Fade>(), fades);
        Assert.Contains(fadeObject3.GetComponent<Fade>(), fades);
    }

    [Test]
    public void GetFades_ShouldReturnEmptyList_WhenNoFadeComponentsAreFound()
    {
        var noFadeObject1 = new GameObject();
        var noFadeObject2 = new GameObject();

        var listOfBlackScreens = new ListOfBlackScreens
        {
            allBlackScreens = new List<GameObject> { noFadeObject1, noFadeObject2 }
        };

        var fades = listOfBlackScreens.GetFades();

        Assert.IsNotNull(fades);
        Assert.AreEqual(0, fades.Count);
    }
}
