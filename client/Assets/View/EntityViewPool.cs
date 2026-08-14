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
    /// <b>Views come in variants, because they are not all interchangeable.</b>
    /// A creep view is built around one unit type's model and one scale, so an
    /// idle Skeleton Warrior cannot stand in for a Minion — handing it over
    /// would draw the wrong body at the wrong size and nothing would throw. So
    /// idle views are kept in one stack per variant and a claim names the
    /// variant it needs. A pool whose views are all alike names none, and works
    /// out of a single stack.
    /// </para>
    /// <para>
    /// <b>Usage is three calls, in order.</b>
    /// <code>
    /// pool.BeginSync();
    /// foreach (var creep in snapshot.Creeps) Pose(pool.Claim(creep.Id, creep.TypeId), creep);
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

        private readonly Dictionary<int, int> _variantOf = new Dictionary<int, int>();

        private readonly Dictionary<int, Stack<T>> _idle = new Dictionary<int, Stack<T>>();

        private readonly HashSet<int> _claimed = new HashSet<int>();

        private readonly List<int> _retiring = new List<int>();

        private readonly Func<int, T> _create;

        private readonly Action<T, bool> _setActive;

        private bool _syncing;

        /// <summary>
        /// A pool whose views are all alike, made by <paramref name="create"/>.
        /// </summary>
        public EntityViewPool(Func<T> create, Action<T, bool> setActive = null)
            : this(Uniform(create), setActive)
        {
        }

        /// <summary>
        /// A pool whose views come in variants.
        /// </summary>
        /// <param name="create">
        /// Makes one view of the variant it is given. Called only when that
        /// variant's idle stack is empty, so this runs once per concurrently-live
        /// entity of a variant over the whole match and not once per entity.
        /// </param>
        /// <param name="setActive">
        /// Shows or hides one view. Defaults to toggling the game object.
        /// Injectable because a test wants to watch this happen without
        /// depending on how it is done.
        /// </param>
        public EntityViewPool(Func<int, T> create, Action<T, bool> setActive = null)
        {
            _create = create ?? throw new ArgumentNullException(nameof(create));
            _setActive = setActive ?? DefaultSetActive;
        }

        /// <summary>The views currently standing for an entity, by id.</summary>
        public IReadOnlyDictionary<int, T> Live => _live;

        /// <summary>How many entities are being drawn.</summary>
        public int LiveCount => _live.Count;

        /// <summary>
        /// How many views are built and waiting, across every variant. The
        /// number that stops growing once the match reaches its busiest moment,
        /// which is the whole point of pooling.
        /// </summary>
        /// <remarks>
        /// Counted on the way out rather than kept as a running total: a
        /// hand-maintained tally is an invariant two call sites have to hold
        /// true, and there are only ever as many stacks here as the match has
        /// kinds of body.
        /// </remarks>
        public int IdleCount
        {
            get
            {
                var waiting = 0;

                foreach (KeyValuePair<int, Stack<T>> variant in _idle)
                {
                    waiting += variant.Value.Count;
                }

                return waiting;
            }
        }

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
        /// frame if there is one, a reused idle one of the same
        /// <paramref name="variant"/> if not, and a newly built one only if
        /// that variant has none waiting.
        /// </summary>
        public T Claim(int id, int variant = 0)
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
                if (_variantOf[id] != variant)
                {
                    throw new InvalidOperationException(
                        "Entity id " + id + " was drawn as variant " + _variantOf[id] + " and is now being "
                        + "claimed as variant " + variant + ". An entity does not change what it is "
                        + "mid-match, so one of the two frames is drawing the wrong thing.");
                }

                return existing;
            }

            T view;

            if (_idle.TryGetValue(variant, out Stack<T> waiting) && waiting.Count > 0)
            {
                view = waiting.Pop();
            }
            else
            {
                view = _create(variant);
                EverCreated++;
            }

            _setActive(view, true);
            _live.Add(id, view);
            _variantOf[id] = variant;

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
            _retiring.Clear();

            foreach (KeyValuePair<int, T> entry in _live)
            {
                if (!_claimed.Contains(entry.Key))
                {
                    _retiring.Add(entry.Key);
                }
            }

            foreach (int id in _retiring)
            {
                Retire(id);
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

            _retiring.Clear();

            foreach (KeyValuePair<int, T> entry in _live)
            {
                _retiring.Add(entry.Key);
            }

            foreach (int id in _retiring)
            {
                Retire(id);
            }
        }

        /// <summary>Hides one view and puts it back on its variant's stack.</summary>
        private void Retire(int id)
        {
            T view = _live[id];
            int variant = _variantOf[id];

            _live.Remove(id);
            _variantOf.Remove(id);
            _setActive(view, false);

            if (!_idle.TryGetValue(variant, out Stack<T> waiting))
            {
                waiting = new Stack<T>();
                _idle.Add(variant, waiting);
            }

            waiting.Push(view);
        }

        /// <summary>
        /// One maker for every variant, for a pool whose views are all alike.
        /// Null-checked here rather than inside the lambda, so a caller that
        /// passes nothing hears about it at construction and not at the first
        /// claim.
        /// </summary>
        private static Func<int, T> Uniform(Func<T> create)
        {
            if (create == null) throw new ArgumentNullException(nameof(create));

            return _ => create();
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
