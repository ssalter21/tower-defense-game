namespace Sim
{
    /// <summary>
    /// The six things a match will tell you about as they happen, if you ask.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Every one of these is purely decorative, and that is a rule about the
    /// shape of this interface rather than a habit.</b> A tower fired: play the
    /// sound and draw the tracer. A creep took damage: pop the number. A creep
    /// died: flash it. A creep leaked: flash the exit. A shot lost its target:
    /// fizzle the trail. One creep drew ahead of another: kick up dust. Nothing
    /// here is state, and nothing here has to be integrated to know what the
    /// world looks like -- everything a view draws game state from is in the
    /// snapshot instead.
    /// </para>
    /// <para>
    /// The bug being engineered out is specific: an effect spawned from an event
    /// and then owning its own lifetime is an effect that cannot be scrubbed
    /// backwards through, because the event that created it is in the past and
    /// nothing will re-emit it. Restricting events to things with no lifetime at
    /// all makes that impossible rather than discouraged. The projectile is the
    /// worked example -- the projectile is a snapshot entity and only its
    /// <i>trail</i> is an event.
    /// </para>
    /// <para>
    /// Every parameter is an entity id or a count. Nothing here carries a
    /// position, a duration or a reference to anything the view could hold on
    /// to, so the rule is enforced by there being nothing to break it with, and
    /// a test asserts that shape rather than trusting this paragraph.
    /// </para>
    /// <para>
    /// Events are emitted <b>only if someone subscribed</b>: they are an
    /// argument to <see cref="Match.Advance(int, IMatchEvents)"/>, not a
    /// property of the match. Re-simulating with nothing passed is how a seek
    /// discards the events of every tick it re-runs, and it needs no discarding
    /// code to do it.
    /// </para>
    /// <para>
    /// <b>Two of the six exist because a landmark table has to be derivable
    /// from this stream and nothing else.</b> The command line runs a match
    /// with nobody pulling a snapshot -- that is the whole of what
    /// instant-resolve is -- so anything it can say about a match has to have
    /// arrived through here. A projectile losing its target and a fast creep
    /// drawing ahead of a slow one are the two moments the sit-down checklist
    /// is written against, and neither of them is visible in a tower firing or
    /// a creep dying. They are reported rather than inferred because inferring
    /// them means a second copy of the flight rule and the spawn schedule
    /// living outside the simulation, going quietly wrong the day either
    /// changes.
    /// </para>
    /// <para>
    /// Neither of them is a rule change: they are told, not decided. Nothing in
    /// the tick loop branches on whether anybody is listening, no dice are
    /// rolled, and the rolling state hash of a match run with these subscribed
    /// is the hash of the same match run in silence -- which is a claim a test
    /// makes rather than a claim this paragraph makes.
    /// </para>
    /// </remarks>
    public interface IMatchEvents
    {
        /// <summary>A tower released a shot at a target.</summary>
        void TowerFired(int towerId, int targetId);

        /// <summary>Damage landed on a creep.</summary>
        void CreepDamaged(int creepId, int amount);

        /// <summary>A creep's health reached zero and it began dying.</summary>
        void CreepDied(int creepId);

        /// <summary>A creep reached the exit.</summary>
        void CreepLeaked(int creepId);

        /// <summary>
        /// A projectile in the air lost the creep it was aimed at, and stopped
        /// existing without landing on anything.
        /// </summary>
        /// <remarks>
        /// The tick this arrives on is the tick the projectile leaves the
        /// snapshot, so a view that fizzles a trail here is fizzling it on the
        /// frame the entity it belonged to went away.
        /// </remarks>
        void ProjectileOrphaned(int projectileId);

        /// <summary>
        /// A creep drew ahead of one that spawned before it -- the moment a
        /// fast group passes a slow one.
        /// </summary>
        /// <remarks>
        /// Ids ascend with spawn order, so <paramref name="creepId"/> is always
        /// the higher of the two. It fires on the tick the pass completes and
        /// once per pair, not for every tick the two stay in that order.
        /// </remarks>
        void CreepOvertook(int creepId, int overtakenCreepId);
    }
}
