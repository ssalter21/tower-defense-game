namespace Sim
{
    /// <summary>
    /// The six things a match will tell you about as they happen, if you ask.
    /// Every parameter is an entity id or a count -- no event carries a
    /// position, a duration, or anything a view could hold on to, and no event
    /// carries simulation state. Emitted only to the sink passed to
    /// <see cref="Match.Advance(int, IMatchEvents)"/>, so an advance that passes
    /// none emits nothing. The tick loop does not branch on whether anybody is
    /// listening and rolls no dice here, so a subscribed match and a silent one
    /// produce the same rolling state hash.
    /// See <c>docs/adr/0008-match-events-are-decorative.md</c>.
    /// </summary>
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
        /// existing without landing on anything. Arrives on the tick the
        /// projectile leaves the snapshot.
        /// </summary>
        void ProjectileOrphaned(int projectileId);

        /// <summary>
        /// A creep drew ahead of one that spawned before it -- the moment a
        /// fast group passes a slow one. Ids ascend with spawn order, so
        /// <paramref name="creepId"/> is always the higher of the two. Fires on
        /// the tick the pass completes, once per pair, not for every tick the
        /// two stay in that order.
        /// </summary>
        void CreepOvertook(int creepId, int overtakenCreepId);
    }
}
