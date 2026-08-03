using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using View;

namespace Tests.PlayMode
{
    /// <summary>
    /// What every view-side fixture needs: a match to look at, and objects that
    /// get destroyed again afterwards.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A play-mode test that leaves a game object behind leaves it for the next
    /// test in the run, which is how a suite acquires an order dependency
    /// nobody wrote and nobody can see. Tracking and destroying is a handful of
    /// lines, and they are here rather than in each fixture so they cannot be
    /// the handful somebody forgot.
    /// </para>
    /// <para>
    /// The split with <see cref="TheMatchOnScreen"/> is where the knowledge
    /// lives: that class knows which models and clips the match is drawn with,
    /// this one knows nothing except how to clean up after itself.
    /// </para>
    /// </remarks>
    public abstract class ViewTest
    {
        private readonly List<GameObject> _spawned = new List<GameObject>();

        [TearDown]
        public void DestroyWhatTheTestSpawned()
        {
            foreach (GameObject spawned in _spawned)
            {
                if (spawned != null) Object.DestroyImmediate(spawned);
            }

            _spawned.Clear();
        }

        /// <summary>An empty object, destroyed when the test ends.</summary>
        protected GameObject Spawn(string name)
        {
            var host = new GameObject(name);
            _spawned.Add(host);

            return host;
        }

        /// <summary>A match, drawn, with nobody watching it.</summary>
        protected MatchView Begin() => TheMatchOnScreen.Begin(Spawn(GetType().Name));

        /// <summary>
        /// Steps the match, drawing every tick, until <paramref name="stop"/>
        /// says so or the match ends.
        /// </summary>
        /// <remarks>
        /// Deliberately not through <see cref="PlaybackController"/>: what most
        /// of these tests want is "get the match into an interesting state",
        /// and going through the wall clock to do it would make every one of
        /// them a test of the clock as well.
        /// </remarks>
        protected static void RunUntil(MatchView view, System.Func<bool> stop)
        {
            while (!view.IsFinished)
            {
                view.StepOneTick();
                view.Draw(1f);

                if (stop())
                {
                    return;
                }
            }
        }
    }
}
