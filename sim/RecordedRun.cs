using System;
using System.Collections.Generic;
using System.Globalization;

namespace Sim
{
    /// <summary>
    /// One round of a played run, ready to be stored, or the sentence saying why
    /// it cannot be.
    /// </summary>
    /// <remarks>
    /// Both halves are here because a caller storing a run has to say what it
    /// stored and what it did not: a round nobody can file is a round that
    /// quietly never enters anybody's field, and silence about it is how a pool
    /// ends up shallower than the runs that fed it.
    /// </remarks>
    public readonly struct StorableRound
    {
        internal StorableRound(int stage, string name, byte[] bytes, string? refusal)
        {
            Stage = stage;
            Name = name;
            Bytes = bytes;
            Refusal = refusal;
        }

        /// <summary>Which round of the run this was, counted from one.</summary>
        public int Stage { get; }

        /// <summary>What the record is called, without a suffix. Empty where it cannot be stored.</summary>
        public string Name { get; }

        /// <summary>The record. Empty where it cannot be stored.</summary>
        public byte[] Bytes { get; }

        /// <summary>Why this round cannot be stored, or null where it can.</summary>
        public string? Refusal { get; }

        /// <summary>Whether there is a record here to write.</summary>
        public bool IsStorable => Refusal is null;

        /// <summary>
        /// The line a person is told about this round, given what the file was
        /// called. A refused round names its reason and no file.
        /// </summary>
        /// <remarks>
        /// The sentence is here rather than in each shell for the reason the
        /// composing is: two callers writing the same fact in two grammars is
        /// two things to read and one of them to drift. Composing a sentence
        /// opens no path, so it sits on this side of ADR-0018 with the rest.
        /// </remarks>
        /// <param name="fileName">What the record was written as, suffix included.</param>
        public string Sentence(string fileName) =>
            IsStorable
                ? "stored     " + fileName
                    + " (" + Bytes.Length.ToString(CultureInfo.InvariantCulture)
                    + " bytes, round " + Stage.ToString(CultureInfo.InvariantCulture)
                    + ", read back before writing)"
                : "not stored " + Refusal;
    }

    /// <summary>
    /// The rounds of a played run, turned into records a folder can hold.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Composing and proving are here; writing is the caller's.</b> Nothing
    /// in this assembly can open a path (ADR-0018), and the two callers that can
    /// -- the shell's <c>--store</c> and the client's proved session -- would
    /// otherwise each hold a copy of what a stored round is made of and what it
    /// costs to prove one.
    /// </para>
    /// <para>
    /// <b>Nothing comes back that will not read.</b> Each record's bytes go
    /// through the reader and out again, and a record whose second set of bytes
    /// is not its first is refused as a fault in this build -- which is
    /// ADR-0050's discipline, and the reason a stored round is never something
    /// somebody finds out about at the draw that meets it.
    /// </para>
    /// </remarks>
    public static class RecordedRun
    {
        /// <summary>
        /// Every round the run resolved, as a record or as a refusal.
        /// </summary>
        /// <remarks>
        /// <b>A round that stood no wall or sent nothing is not storable, and
        /// says so.</b> A stored round is a wall and a wave; a defense record
        /// with no towers and a wave record with no orders are both refused
        /// where they are read, so composing one would be composing bytes the
        /// folder would refuse tomorrow.
        /// </remarks>
        /// <param name="run">The run, after every round of it resolved.</param>
        /// <param name="map">The board it was played on, which pins the geometry by hash.</param>
        /// <param name="types">The roster both halves are recorded against.</param>
        /// <param name="mapHandle">
        /// Which map the walls claim to be on, for looking one up. A pool is
        /// indexed by hash, so <see cref="GhostRecord.NoMapHandle"/> is the
        /// honest answer where nothing files maps.
        /// </param>
        public static IReadOnlyList<StorableRound> Of(
            Run run,
            HexMap map,
            UnitTypeTable types,
            int mapHandle = GhostRecord.NoMapHandle)
        {
            if (run is null)
            {
                throw new ArgumentNullException(nameof(run));
            }

            var rounds = new StorableRound[run.Sent.Count];

            for (int round = 0; round < rounds.Length; round++)
            {
                rounds[round] = Composed(run.Sent[round], map, types, round + 1, mapHandle);
            }

            return rounds;
        }

        /// <summary>One round, composed and proved, or refused by name.</summary>
        private static StorableRound Composed(
            RoundOrders orders,
            HexMap map,
            UnitTypeTable types,
            int stage,
            int mapHandle)
        {
            if (orders.Defense.Count == 0 || orders.Wave.Count == 0)
            {
                return new StorableRound(
                    stage,
                    string.Empty,
                    new byte[0],
                    "round "
                    + stage.ToString(CultureInfo.InvariantCulture)
                    + " stood "
                    + orders.Defense.Count.ToString(CultureInfo.InvariantCulture)
                    + " towers and sent "
                    + orders.Wave.Count.ToString(CultureInfo.InvariantCulture)
                    + " orders. A stored round is a wall and a wave, and neither half may be empty.");
            }

            byte[] bytes = RoundRecord
                .Of(map, orders.Defense, orders.Wave, types, stage, mapHandle)
                .ToBytes();

            RequireReadBack(stage, bytes);

            return new StorableRound(stage, StoredRounds.NameOf(bytes), bytes, refusal: null);
        }

        /// <summary>The bytes, read and written again, refused where the two disagree.</summary>
        private static void RequireReadBack(int stage, byte[] bytes)
        {
            string name = "the stored round of wave " + stage.ToString(CultureInfo.InvariantCulture);
            byte[] again = RoundRecord.FromBytes(name, bytes).ToBytes();

            if (again.Length == bytes.Length)
            {
                bool same = true;

                for (int index = 0; index < bytes.Length && same; index++)
                {
                    same = bytes[index] == again[index];
                }

                if (same)
                {
                    return;
                }
            }

            throw new SimulationException(
                "The record of round "
                + stage.ToString(CultureInfo.InvariantCulture)
                + " does not write back to the bytes it was read from, so its id is not the id of what it "
                + "says. A record is addressed by its own bytes, and a writer that cannot reproduce them "
                + "is a fault in this build rather than in anything stored.");
        }
    }
}
