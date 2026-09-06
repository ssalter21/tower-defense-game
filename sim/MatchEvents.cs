namespace Sim
{
    /// <summary>
    /// The ten things a match will tell you about as they happen, if you ask.
    /// Every parameter is an entity id, a count, or a value read straight off
    /// the emitter's row -- no event carries a position, a duration, or
    /// simulation state. Emitted only to the sink passed to
    /// <see cref="Match.Advance(int, IMatchEvents)"/>, and a subscribed match
    /// produces the same rolling state hash as a silent one. See
    /// <c>docs/adr/0008-match-events-are-decorative.md</c>.
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
        /// A creep became another row, on the tick the change resolved.
        /// </summary>
        /// <remarks>
        /// <b>The body it is now drawn as is not this event.</b> Which row a
        /// creep is is a field of the snapshot, so a seek that lands either side
        /// of this tick draws the right body without anybody having heard
        /// anything -- see <c>docs/adr/0007-snapshot-is-the-only-view-input.md</c>.
        /// What this carries is the moment, for whatever a view wants to mark it
        /// with, and the row it names is the one the snapshot is already
        /// reporting.
        /// </remarks>
        /// <param name="creepId">The body that changed. It keeps its id.</param>
        /// <param name="typeId">The row it is now, by its stable id.</param>
        void CreepTransformed(int creepId, int typeId);

        /// <summary>
        /// A creep put another body on the corridor beside itself, on the tick
        /// the raise fired.
        /// </summary>
        /// <remarks>
        /// <b>The body itself is not this event.</b> A raised creep is an entity
        /// in the snapshot from the tick it arrives, so a seek that lands either
        /// side of this tick draws the right bodies without anybody having heard
        /// anything -- see <c>docs/adr/0007-snapshot-is-the-only-view-input.md</c>.
        /// What this carries is the moment and which two bodies it joins, for
        /// whatever a view wants to mark it with.
        /// </remarks>
        /// <param name="creepId">The body that raised it.</param>
        /// <param name="raisedCreepId">The body it put on the corridor.</param>
        void CreepRaised(int creepId, int raisedCreepId);

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

        /// <summary>
        /// A bubble that fires with an attack went off, on the tick the shot
        /// resolved.
        /// </summary>
        /// <remarks>
        /// <b>The centre is an entity and not a place.</b> A sweep is centred
        /// on the tower that fired it and a blast on the body the shot arrived
        /// at, and both are ids out of the one id space -- so a listener looks
        /// the position up in the snapshot it is drawing rather than being
        /// handed one that could disagree with it. A blast whose target is no
        /// longer on the map has no centre at all and is not reported, which is
        /// the same silence its damage lands in.
        /// </remarks>
        /// <param name="centreId">The tower for a sweep, the creep the shot arrived at for a blast.</param>
        /// <param name="radiusMilliHex">Thousandths of a hex, read as a sphere. Zero is the centre alone.</param>
        /// <param name="payload">What it carried into everything it enclosed.</param>
        void BlastLanded(int centreId, int radiusMilliHex, BubblePayload payload);

        /// <summary>
        /// A bubble on a clock of its own pulsed, on the tick its period came
        /// round.
        /// </summary>
        /// <remarks>
        /// Centred on whatever is emitting it, which is a tower or a walking
        /// creep depending on which row carries the aura -- one id space, so
        /// the parameter does not have to say which.
        /// </remarks>
        /// <param name="emitterId">The tower or creep the aura pulses from.</param>
        /// <param name="radiusMilliHex">Thousandths of a hex, read as a sphere. Zero is the emitter alone.</param>
        /// <param name="payload">What the pulse carried into everything it enclosed.</param>
        void AuraPulsed(int emitterId, int radiusMilliHex, BubblePayload payload);
    }
}
