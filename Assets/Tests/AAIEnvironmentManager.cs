using NUnit.Framework;
using UnityEngine;
using Unity.MLAgents.Sensors;
using System.Collections.Generic;
using System;
using MLAgents;

namespace Tests
{
    public class AAI3EnvironmentManagerTests
    {
        private AAI3EnvironmentManager _environmentManager;
        private GameObject _arenaObject;
        private GameObject _uiCanvas;

        [SetUp]
        public void Setup()
        {
            _arenaObject = new GameObject();
            _uiCanvas = new GameObject();

            _environmentManager = new GameObject().AddComponent<AAI3EnvironmentManager>();
            _environmentManager.arena = _arenaObject;
            _environmentManager.uiCanvas = _uiCanvas;
            _environmentManager.configFile = "testConfig";
            _environmentManager.maximumResolution = 512;
            _environmentManager.minimumResolution = 4;
            _environmentManager.defaultResolution = 84;
            _environmentManager.defaultRaysPerSide = 2;
            _environmentManager.defaultRayMaxDegrees = 60;
            _environmentManager.defaultDecisionPeriod = 3;
        }

        [Test]
        public void InitializesArenasParametersSideChannel()
        {
            _environmentManager.Awake();
            Assert.IsNotNull(_environmentManager._arenasParametersSideChannel);
        }

        [Test]
        public void InstantiatesArenasCorrectly()
        {
            _environmentManager.InstantiateArenas();
            Assert.IsNotNull(_environmentManager._instantiatedArena);
            Assert.AreEqual(0, _environmentManager._instantiatedArena.arenaID);
        }

        [Test]
        public void ParsesEnvironmentParametersCorrectly()
        {
            var args = new string[] { "--playerMode", "1", "--resolution", "128" };
            var parsedParameters = _environmentManager.RetrieveEnvironmentParameters(args);
            Assert.IsTrue(parsedParameters.ContainsKey("playerMode"));
            Assert.IsTrue(parsedParameters.ContainsKey("resolution"));
            Assert.AreEqual(1, parsedParameters["playerMode"]);
            Assert.AreEqual(128, parsedParameters["resolution"]);
        }

        [Test]
        public void GetsMaxArenaIDCorrectly()
        {
            int maxArenaID = _environmentManager.getMaxArenaID();
            Assert.AreEqual(0, maxArenaID); // no arenas instantiated initially
        }

        [Test]
        public void RandomizeArenasStatusCheck()
        {
            bool status = _environmentManager.GetRandomizeArenasStatus();
            Assert.IsFalse(status); // randomizeArenas is false by default
        }

        [Test]
        public void ChangesRayCastsCorrectly()
        {
            var raySensor = new RayPerceptionSensorComponent3D();
            _environmentManager.ChangeRayCasts(raySensor, 5, 90);
            Assert.AreEqual(5, raySensor.RaysPerDirection);
            Assert.AreEqual(90, raySensor.MaxRayDegrees);
        }

        [Test]
        public void ChangesResolutionCorrectly()
        {
            var cameraSensor = new CameraSensorComponent();
            _environmentManager.ChangeResolution(cameraSensor, 128, 128, true);
            Assert.AreEqual(128, cameraSensor.Width);
            Assert.AreEqual(128, cameraSensor.Height);
            Assert.IsTrue(cameraSensor.Grayscale);
        }

        [TearDown]
        public void TearDown()
        {
            GameObject.Destroy(_environmentManager.gameObject);
            GameObject.Destroy(_arenaObject);
            GameObject.Destroy(_uiCanvas);
        }
    }
}
