namespace Sim
{
    /// <summary>
    /// The four things a match will tell you about as they happen, if you ask.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Every one of these is purely decorative, and that is a rule about the
    /// shape of this interface rather than a habit.</b> A tower fired: play the
    /// sound and draw the tracer. A creep took damage: pop the number. A creep
    /// died: flash it. A creep leaked: flash the exit. Nothing here is state,
    /// and nothing here has to be integrated to know what the world looks like
    /// -- everything a view draws game state from is in the snapshot instead.
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
    }
}
