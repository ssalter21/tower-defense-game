using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using View;
using View.Editor;

namespace Tests.EditMode
{
    /// <summary>
    /// The scene holds exactly one root object, and this is the test that says
    /// so out loud.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>One root is a rule, and the alternative is a budget.</b> "A few root
    /// objects" is a number nobody can defend, so it gets argued upward one
    /// convenient object at a time — a manager, a light probe volume, an audio
    /// listener somebody needed once — until the scene is where the game is
    /// decided, in serialized YAML that cannot be reviewed and merges by luck.
    /// One is the only count that can be checked by counting.
    /// </para>
    /// <para>
    /// <b>This has to be an edit-mode test.</b> The claim is about a file on
    /// disk. By the time a play-mode run has loaded the scene, the test runner
    /// has added roots of its own and the count means nothing.
    /// </para>
    /// <para>
    /// The failure was watched: adding a second empty object to the scene and
    /// saving it turns
    /// <see cref="TheSceneHoldsExactlyOneRootObject"/> red, naming both roots.
    /// </para>
    /// </remarks>
    public class SceneRootTests
    {
        private Scene _scene;

        /// <summary>
        /// Opened additively and closed again, so the test runner's own scene
        /// survives. Loading it singly would tear down whatever the runner is
        /// standing on, which is a flaky test rather than a strict one.
        /// </summary>
        [SetUp]
        public void OpenTheScene()
        {
            _scene = EditorSceneManager.OpenScene(MatchSceneBuilder.ScenePath, OpenSceneMode.Additive);

            Assert.That(
                _scene.IsValid(),
                Is.True,
                "Could not open " + MatchSceneBuilder.ScenePath
                + ". Regenerate it with tools/build-match-scene.ps1.");
        }

        [TearDown]
        public void CloseTheScene()
        {
            if (_scene.IsValid())
            {
                EditorSceneManager.CloseScene(_scene, removeScene: true);
            }
        }

        [Test]
        public void TheSceneHoldsExactlyOneRootObject()
        {
            TheOnlyRoot();
        }

        /// <summary>
        /// The root, and the count assertion that has to hold before anything
        /// else in this file means anything.
        /// </summary>
        /// <remarks>
        /// Written out rather than left to <c>Single()</c>, whose failure is
        /// "Sequence contains more than one element" — true, unhelpful, and
        /// silent about which objects. Every test here goes through it, so a
        /// second root reports itself by name three times rather than once.
        /// </remarks>
        private GameObject TheOnlyRoot()
        {
            GameObject[] roots = _scene.GetRootGameObjects();

            Assert.That(
                roots.Length,
                Is.EqualTo(1),
                "The scene has " + roots.Length + " root objects: "
                + string.Join(", ", roots.Select(root => root.name))
                + ". It is allowed exactly one, and everything else hangs off it.");

            return roots[0];
        }

        [Test]
        public void TheOneRootIsTheMatchRoot()
        {
            GameObject root = TheOnlyRoot();

            Assert.That(root.name, Is.EqualTo(SceneFraming.RootObjectName));
            Assert.That(root.GetComponent<MatchRoot>(), Is.Not.Null, "The root carries MatchRoot and builds the rest.");
        }

        /// <summary>
        /// The camera and the light are built at runtime from committed
        /// constants, so neither is allowed to exist in the scene asset. A
        /// camera dragged in "just to see something" is the exact regression
        /// this rules out: it would work, it would look right, and its framing
        /// would live in YAML that no diff can be read.
        /// </summary>
        [Test]
        public void NoCameraAndNoLightAreAuthoredIntoTheScene()
        {
            GameObject root = TheOnlyRoot();

            Assert.That(
                root.GetComponentsInChildren<Camera>(includeInactive: true),
                Is.Empty,
                "The camera is built at runtime by OrbitCameraRig, from SceneFraming.");

            Assert.That(
                root.GetComponentsInChildren<Light>(includeInactive: true),
                Is.Empty,
                "The light is built at runtime by MatchRoot, from SceneFraming.");

            Assert.That(
                root.transform.childCount,
                Is.EqualTo(0),
                "Nothing is authored under the root either -- the floor, the light and the camera "
                + "are all built at runtime.");
        }

        /// <summary>
        /// A double-clickable build has to open on something. The scene builder
        /// puts this scene at index zero and there is no second one.
        /// </summary>
        [Test]
        public void TheMatchSceneIsTheOnlySceneInTheBuild()
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;

            Assert.That(scenes.Length, Is.EqualTo(1), "There is one scene, and it is the match.");
            Assert.That(scenes[0].path, Is.EqualTo(MatchSceneBuilder.ScenePath));
            Assert.That(scenes[0].enabled, Is.True);
        }
    }
}
