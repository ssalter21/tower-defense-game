namespace Sim
{
    /// <summary>
    /// The six things a match will tell you about as they happen, if you ask.
    /// Every parameter is an entity id or a count -- no event carries a
    /// position, a duration, or simulation state. Emitted only to the sink
    /// passed to <see cref="Match.Advance(int, IMatchEvents)"/>, and a
    /// subscribed match produces the same rolling state hash as a silent one.
    /// See <c>docs/adr/0008-match-events-are-decorative.md</c>.
    /// </summary>
    public interface IMatchEvents
    {
        void TowerFired(int towerId, int targetId);

        void CreepDamaged(int creepId, int amount);

        /// <summary>Health reached zero and the creep began dying.</summary>
        void CreepDied(int creepId);

        /// <summary>The creep reached the exit.</summary>
        void CreepLeaked(int creepId);

        /// <summary>
        /// A projectile lost the creep it was aimed at and stopped existing
        /// without landing. Arrives on the tick it leaves the snapshot.
        /// </summary>
        void ProjectileOrphaned(int projectileId);

        /// <summary>
        /// A creep drew ahead of one that spawned before it. Ids ascend with
        /// spawn order, so <paramref name="creepId"/> is always the higher of
        /// the two. Fires once per pair, on the tick the pass completes.
        /// </summary>
        void CreepOvertook(int creepId, int overtakenCreepId);
    }
}
