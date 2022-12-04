﻿using NUnit.Framework;
using Sisus.Init;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;

namespace Init.Demo.Tests
{
    /// <summary>
    /// Unit tests for <see cref="Spawner"/>.
    /// </summary>
    public sealed class TestSpawner
    {
        private SpawnerSettings spawnerSettings;
        private string prefabInstanceName;
        private Spawner spawner;
        private TestTimeProvider testTimeProvider;
        private IUpdate updateable;

        [SetUp]
        public void Setup()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            EditorCoroutine.StopAll();

            const string prefabName = "Spawned";
            var prefab = new GameObject(prefabName);
            prefabInstanceName = prefabName + "(Clone)";
            spawnerSettings = new SpawnerSettings();
            testTimeProvider = new TestTimeProvider();
            Trackable trackable = new Trackable();
            ILevel level = new Level();
            IBuilder<GameObject> gameObjectBuilder = new EditModeGameObjectBuilder(prefab);

            spawner = new Spawner(spawnerSettings, trackable, level, testTimeProvider, gameObjectBuilder);
            spawner.CoroutineRunner = EditorCoroutineRunner.sharedInstance;
            updateable = spawner;
        }

        [TearDown]
        public void TearDown()
        {
            spawner = null;
            updateable = null;
            EditorCoroutine.StopAll();
        }

        [Test]
        public void SpawnsObject()
        {
            Assert.IsNull(GameObject.Find(prefabInstanceName));

            testTimeProvider.Time = spawnerSettings.MaxSpawnInterval + 1f;

            updateable.Update(spawnerSettings.MaxSpawnInterval + 1f);

            EditorCoroutine.MoveAllNext(true);

            Assert.IsNotNull(GameObject.Find(prefabInstanceName));
        }

        [Test]
        public void DespawnsObject()
        {
            Assert.IsNull(GameObject.Find(prefabInstanceName));

            testTimeProvider.Time = spawnerSettings.MaxSpawnInterval + 1f;

            updateable.Update(spawnerSettings.MaxSpawnInterval + 1f);

            EditorCoroutine.MoveAllNext(true);

            Assert.IsNotNull(GameObject.Find(prefabInstanceName));

            testTimeProvider.Time += spawnerSettings.LifeTime + 1f;

            EditorCoroutine.FastForwardAll();

            Assert.IsNull(GameObject.Find(prefabInstanceName));
        }

        private class Trackable : ITrackable
        {
            public Vector2 Position => Vector2.zero;
            public event UnityAction PositionChanged { add { } remove { } }
        }

        private class Level : ILevel
        {
            public RectInt Bounds => new RectInt(0, 0, 10, 10);
        }
    }
}