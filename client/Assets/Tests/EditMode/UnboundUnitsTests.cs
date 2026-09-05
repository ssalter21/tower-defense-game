using System;
using System.Linq;
using NUnit.Framework;
using Tests.Fixtures;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using View;
using View.Editor;

namespace Tests.EditMode
{
    /// <summary>
    /// The allowance that lets a row of <c>content/units.txt</c> exist before
    /// its art does: what a listed row draws as, and that an unlisted one still
    /// refuses to draw at all.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Most of this is empty at rest and that is the point.</b>
    /// <see cref="UnboundUnits.Rows"/> is empty whenever the roster and its art
    /// agree, so the tests that walk it assert about nothing until somebody
    /// lands a row ahead of its model. The ones that do not walk it — the
    /// stand-in loading, a stand-in built by hand, the throw for a row with no
    /// allowance — hold the mechanism itself, which is what has to be right on
    /// the day the list stops being empty.
    /// </para>
    /// <para>
    /// A unit id nothing has ever used stands for "unlisted and unbound".
    /// <c>content/units.txt</c> numbers its rows well below it, so the throw
    /// this asserts is the one a forgotten row would get.
    /// </para>
    /// </remarks>
    public class UnboundUnitsTests
    {
        /// <summary>An id on neither art table and on no allowance.</summary>
        private const int StrangerUnitId = 9999;

        [Test]
        public void TheStandInIsImportedWhereItIsDeclaredToBe()
        {
            Assert.That(
                AssetDatabase.LoadAssetAtPath<GameObject>(UnboundUnits.StandInModelPath),
                Is.Not.Null,
                UnboundUnits.StandInModelPath + " is not in the project, so a row with no art yet "
                + "could not be drawn at all.");

            Assert.That(UnboundUnits.StandInModel(), Is.Not.Null);
        }

        /// <summary>
        /// A stand-in is the stand-in model at the size asked for, holding
        /// nothing and posed by nothing.
        /// </summary>
        /// <remarks>
        /// Built here for an id that is not on the list, because the list is
        /// empty at rest and a test that only walked it would pass by never
        /// running. Both sizes are asked for: creep and tower are the two a
        /// listed row can be given.
        /// </remarks>
        [Test]
        public void ARowWithNoArtYetDrawsTheStandInAtTheSizeItWasGiven(
            [Values(MatchArt.CreepScale, MatchArt.TowerScale)] float scale)
        {
            UnitArt art = UnboundUnits.StandIn(StrangerUnitId, scale);

            Assert.That(art.UnitId, Is.EqualTo(StrangerUnitId));
            Assert.That(art.Model, Is.SameAs(UnboundUnits.StandInModel()));
            Assert.That(art.Scale, Is.EqualTo(scale));
            Assert.That(art.IsComplete, Is.True, "a stand-in that draws at no size never appears");

            Assert.That(art.RightHand, Is.Null);
            Assert.That(art.LeftHand, Is.Null);
            Assert.That(art.IsPosed, Is.False, "an undressed row stands in its bind pose");
        }

        /// <summary>
        /// Both art tables draw every listed row as the stand-in, and no row is
        /// on a table and on the list at once.
        /// </summary>
        /// <remarks>
        /// The two tables are the scene builder's and the fixture's, which are
        /// separate transcriptions of every art assignment. A row that appeared
        /// in both a table and the list would have art and an excuse for having
        /// none, and which of the two won would be an ordering accident.
        /// </remarks>
        [Test]
        public void EveryRowWithNoArtYetDrawsTheStandInInBothArtTables()
        {
            MatchArt chosen = ChosenArt.Load();
            MatchArt built = MatchSceneBuilder.Art();

            foreach ((int unitId, float scale) in UnboundUnits.Rows)
            {
                Assert.That(
                    ChosenArt.UnitPaths.Any(u => u.unitId == unitId),
                    Is.False,
                    "unit " + unitId + " has art chosen for it and is also listed as having none");

                foreach (MatchArt art in new[] { chosen, built })
                {
                    Assert.That(art.ModelFor(unitId), Is.SameAs(UnboundUnits.StandInModel()));
                    Assert.That(art.ScaleFor(unitId), Is.EqualTo(scale));
                }
            }
        }

        /// <summary>
        /// The committed scene draws every listed row as the stand-in too.
        /// </summary>
        /// <remarks>
        /// The scene is where a build gets its art, and it is generated: a row
        /// added to the list after the last <c>tools/build-match-scene.ps1</c>
        /// is in every in-memory table and in nothing a player would run.
        /// </remarks>
        [Test]
        public void TheGeneratedSceneDrawsEveryRowWithNoArtYetAsTheStandIn()
        {
            if (UnboundUnits.Rows.Length == 0)
            {
                Assert.Pass("No row is waiting for art.");
            }

            UnityEngine.SceneManagement.Scene scene =
                EditorSceneManager.OpenScene(MatchSceneBuilder.ScenePath, OpenSceneMode.Additive);

            try
            {
                MatchRoot root = scene.GetRootGameObjects()
                    .Select(o => o.GetComponent<MatchRoot>())
                    .FirstOrDefault(r => r != null);

                Assert.That(root, Is.Not.Null, "no MatchRoot in " + MatchSceneBuilder.ScenePath);

                foreach ((int unitId, float scale) in UnboundUnits.Rows)
                {
                    Assert.That(
                        root.Art.ModelFor(unitId),
                        Is.SameAs(UnboundUnits.StandInModel()),
                        "unit " + unitId + " is listed as having no art yet and the scene draws it as "
                        + "something else. Run tools/build-match-scene.ps1 and commit what it writes.");

                    Assert.That(root.Art.ScaleFor(unitId), Is.EqualTo(scale));
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, removeScene: true);
            }
        }

        /// <summary>
        /// A row with neither art nor an allowance still throws, naming itself.
        /// </summary>
        /// <remarks>
        /// This is the half of the allowance that has to keep working. A list of
        /// exceptions is only worth having if what is not on it still fails, and
        /// fails loudly enough to say which row was forgotten.
        /// </remarks>
        [Test]
        public void ARowWithNeitherArtNorAnAllowanceThrowsByName()
        {
            Assert.That(UnboundUnits.Lists(StrangerUnitId), Is.False);

            foreach (MatchArt art in new[] { ChosenArt.Load(), MatchSceneBuilder.Art() })
            {
                var thrown = Assert.Throws<InvalidOperationException>(() => art.ArtFor(StrangerUnitId));

                Assert.That(thrown.Message, Does.Contain(StrangerUnitId.ToString()));
            }
        }
    }
}
