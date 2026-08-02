using System;
using System.Collections.Generic;
using UnityEngine;

namespace View
{
    /// <summary>
    /// Pooled view objects, held by entity id, released by nobody claiming them.
    /// </summary>
    /// <typeparam name="T">The component each entity is drawn with.</typeparam>
    /// <remarks>
    /// <para>
    /// <b>There is exactly one way an object goes back in the pool: its id
    /// stopped appearing.</b> No despawn message, no death callback, no
    /// "remember to release it" at any call site. The snapshot is a complete
    /// statement of what exists, so the set of ids in it is the set of objects
    /// that should be alive, and everything else follows by subtraction.
    /// </para>
    /// <para>
    /// This is not an optimisation, it is the whole design. A second
    /// bookkeeping path -- an event that says "this one is gone", a flag on the
    /// view, a timer -- is a second opinion about what exists, and the two
    /// disagree exactly when something interesting happened: a projectile whose
    /// target died mid-flight, a creep removed on the tick a scrub jumped over.
    /// Those are the cases that produce a ghost object nobody can explain,
    /// still sitting on the playfield with no simulation entity behind it.
    /// Subtraction cannot produce one.
    /// </para>
    /// <para>
    /// It also makes seeking free. Scrub to any tick, pull a snapshot, sync:
    /// whatever is not in that snapshot goes away, whatever is new arrives, and
    /// no code anywhere had to know a seek happened.
    /// </para>
    /// <para>
    /// <b>Usage is three calls, in order.</b>
    /// <code>
    /// pool.BeginSync();
    /// foreach (var creep in snapshot.Creeps) Pose(pool.Claim(creep.Id), creep);
    /// pool.EndSync();
    /// </code>
    /// Claiming an id twice in one sync is a caller bug and throws, because the
    /// only way it happens is two entities sharing an id, which would mean one
    /// of them is invisible and the other is flickering between two states.
    /// </para>
    /// </remarks>
    public sealed class EntityViewPool<T>
        where T : Component
    {
        private readonly Dictionary<int, T> _live = new Dictionary<int, T>();

        private readonly Stack<T> _idle = new Stack<T>();

        private readonly HashSet<int> _claimed = new HashSet<int>();

        private readonly List<int> _departed = new List<int>();

        private readonly Func<T> _create;

        private readonly Action<T, bool> _setActive;

        private bool _syncing;

        /// <summary>
        /// A pool that makes new views with <paramref name="create"/>.
        /// </summary>
        /// <param name="create">
        /// Makes one view. Called only when the idle stack is empty, so this
        /// runs once per concurrently-live entity over the whole match and not
        /// once per entity.
        /// </param>
        /// <param name="setActive">
        /// Shows or hides one view. Defaults to toggling the game object.
        /// Injectable because a test wants to watch this happen without
        /// depending on how it is done.
        /// </param>
        public EntityViewPool(Func<T> create, Action<T, bool> setActive = null)
        {
            _create = create ?? throw new ArgumentNullException(nameof(create));
            _setActive = setActive ?? DefaultSetActive;
        }

        /// <summary>The views currently standing for an entity, by id.</summary>
        public IReadOnlyDictionary<int, T> Live => _live;

        /// <summary>How many entities are being drawn.</summary>
        public int LiveCount => _live.Count;

        /// <summary>
        /// How many views are built and waiting. The number that stops growing
        /// once the match reaches its busiest moment, which is the whole point
        /// of pooling.
        /// </summary>
        public int IdleCount => _idle.Count;

        /// <summary>
        /// How many views this pool has ever built. A test watches this stop
        /// climbing; if it tracks the number of entities the match has ever had,
        /// nothing is being reused.
        /// </summary>
        public int EverCreated { get; private set; }

        /// <summary>Starts a sync. Nothing is claimed yet.</summary>
        public void BeginSync()
        {
            if (_syncing)
            {
                throw new InvalidOperationException(
                    "BeginSync was called twice without an EndSync between them. A sync that never ended "
                    + "released nothing, so every entity that vanished during it is still on the playfield.");
            }

            _syncing = true;
            _claimed.Clear();
        }

        /// <summary>
        /// The view standing for <paramref name="id"/> — the one from last
        /// frame if there is one, a reused idle one if not, and a newly built
        /// one only if the pool is empty.
        /// </summary>
        public T Claim(int id)
        {
            if (!_syncing)
            {
                throw new InvalidOperationException(
                    "Claim was called outside a sync. Without BeginSync there is no record of what was "
                    + "claimed, so EndSync would release everything that is still alive.");
            }

            if (!_claimed.Add(id))
            {
                throw new InvalidOperationException(
                    "Entity id " + id + " was claimed twice in one sync. Two entities sharing an id means "
                    + "one of them is not being drawn and the other is being posed twice.");
            }

            if (_live.TryGetValue(id, out T existing))
            {
                return existing;
            }

            T view;

            if (_idle.Count > 0)
            {
                view = _idle.Pop();
            }
            else
            {
                view = _create();
                EverCreated++;
            }

            _setActive(view, true);
            _live.Add(id, view);

            return view;
        }

        /// <summary>
        /// Ends the sync: every id that was live and was not claimed has
        /// stopped appearing, so its view goes back in the pool.
        /// </summary>
        public void EndSync()
        {
            if (!_syncing)
            {
                throw new InvalidOperationException("EndSync was called without a BeginSync.");
            }

            _syncing = false;
            _departed.Clear();

            foreach (KeyValuePair<int, T> entry in _live)
            {
                if (!_claimed.Contains(entry.Key))
                {
                    _departed.Add(entry.Key);
                }
            }

            foreach (int id in _departed)
            {
                T view = _live[id];
                _live.Remove(id);
                _setActive(view, false);
                _idle.Push(view);
            }
        }

        /// <summary>
        /// Releases everything, as though an empty snapshot had arrived. What a
        /// seek to before the match started does, and what tearing the view
        /// down does.
        /// </summary>
        public void ReleaseAll()
        {
            if (_syncing)
            {
                _syncing = false;
                _claimed.Clear();
            }

            foreach (KeyValuePair<int, T> entry in _live)
            {
                _setActive(entry.Value, false);
                _idle.Push(entry.Value);
            }

            _live.Clear();
        }

        private static void DefaultSetActive(T view, bool active)
        {
            if (view != null)
            {
                view.gameObject.SetActive(active);
            }
        }
    }
}
