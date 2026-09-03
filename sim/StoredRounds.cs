using System;
using System.Collections.Generic;
using System.Globalization;

namespace Sim
{
    /// <summary>
    /// The rounds somebody has stored, indexed the way a run draws them: by
    /// stage, with the id each one was read out of beside it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Bytes in, a population out, and no path anywhere.</b> Whoever holds
    /// the folder opens the files and hands the bytes over one at a time; what
    /// happens to them -- the gates, the indexing, the refusals -- is here, so
    /// the shell and the client cannot come to two different opinions about
    /// which stored rounds a run may meet. ADR-0018 is why the halves are split
    /// here rather than a directory walk being written twice.
    /// </para>
    /// <para>
    /// <b>A record this refuses is named and skipped, never thrown.</b> A folder
    /// is a runtime artefact that accumulates over months, so a record from a
    /// retired format or a file somebody truncated is the ordinary case rather
    /// than an emergency. What is refused is refused whole, per ADR-0013: there
    /// is no partial read of a stored round, and nothing is repaired.
    /// </para>
    /// <para>
    /// <b>A stage is ordered by id, here, whatever order the records arrived
    /// in.</b> A draw is an index into a stage, so two callers whose
    /// directories listed the same files differently would draw different
    /// fields off one seed. Ordering it here rather than asking each caller to
    /// sort is what stops that being a rule two shells have to keep; an id is a
    /// fact about the bytes, so the order is the same everywhere.
    /// </para>
    /// <para>
    /// <b>The ids travel beside the members rather than inside them.</b> A
    /// record carries no id (ADR-0030) and a run is handed a population rather
    /// than bytes, so naming what a round met is a lookup on this side.
    /// </para>
    /// </remarks>
    public sealed class StoredRounds
    {
        private readonly HexMap _map;

        private readonly UnitTypeTable _types;

        private readonly List<List<RoundOrders>> _stages = new List<List<RoundOrders>>();

        private readonly List<List<Hash64>> _ids = new List<List<Hash64>>();

        private readonly List<string> _refusals = new List<string>();

        /// <summary>An empty pool, to be filled a record at a time.</summary>
        /// <param name="map">The board a stored round has to have been played on.</param>
        /// <param name="types">The roster it has to have been played against.</param>
        public StoredRounds(HexMap map, UnitTypeTable types)
        {
            _map = map ?? throw new ArgumentNullException(nameof(map));
            _types = types ?? throw new ArgumentNullException(nameof(types));
        }

        /// <summary>The population at stage one, then stage two, and so on.</summary>
        public IReadOnlyList<IReadOnlyList<RoundOrders>> ByStage => _stages;

        /// <summary>How many stages have anybody at them.</summary>
        public int Stages => _stages.Count;

        /// <summary>How many rounds were taken, over every stage.</summary>
        public int Count { get; private set; }

        /// <summary>What was offered and was not taken, one sentence each.</summary>
        public IReadOnlyList<string> Refusals => _refusals;

        /// <summary>
        /// What a stored round is called: the hexadecimal of its own id, which
        /// is the hash of its own bytes.
        /// </summary>
        /// <remarks>
        /// The naming rule is here rather than in whoever writes the file so
        /// that the writer and the reader cannot disagree about it. What comes
        /// back is a name and not a path: a suffix and a directory are the
        /// caller's, and this assembly has neither.
        /// </remarks>
        public static string NameOf(byte[] bytes) => RecordId.Of(bytes).ToString();

        /// <summary>
        /// One stored round's bytes, taken into the population or refused by
        /// name.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Four gates. The name must be the id of the bytes it is on, or two
        /// files could hold one record and a field would meet it twice. The
        /// bytes must read. The three stamps must be this board, this build and
        /// this roster -- declared to <see cref="ReplayGate"/> as every other
        /// record kind declares its own, so a stored round is retired by the
        /// same walk and the same sentence a bundle is. And both halves must
        /// resolve against the roster, which is where a type id nothing defines
        /// refuses.
        /// </para>
        /// <para>
        /// <b>Both refusals are caught and neither is repaired.</b> Reading and
        /// replaying are separate gates (ADR-0014) and their exceptions share no
        /// base, so both are named here: what is different about a folder is
        /// only what is done with the refusal.
        /// </para>
        /// <para>
        /// <b>Nothing here throws for a bad record</b>, which is the whole
        /// difference between this and a read gate. A refusal is a sentence on
        /// <see cref="Refusals"/> and the pool goes on filling.
        /// </para>
        /// </remarks>
        /// <param name="name">
        /// What the record is called where it was stored, without any suffix.
        /// </param>
        /// <param name="bytes">The record itself.</param>
        public void Add(string name, byte[] bytes)
        {
            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (bytes is null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            Hash64 id = RecordId.Of(bytes);

            if (!string.Equals(name, id.ToString(), StringComparison.Ordinal))
            {
                _refusals.Add(
                    name
                    + " is named after a record it does not hold. A stored round is named by the hash of "
                    + "its own bytes, and these hash to "
                    + id.ToString()
                    + ".");

                return;
            }

            try
            {
                RoundRecord record = RoundRecord.FromBytes(name, bytes);

                ReplayGate.Require(
                    Stamp.Of("simulation version", record.Header.SimVersion, SimulationVersion.Current),
                    Stamp.Of("content", record.Header.ContentHash, _types.ContentHash),
                    Stamp.Of("map", record.MapHash, _map.MapHash));

                Take(id, record.Stage, record.ToOrders(_types));
            }
            catch (RecordException refused)
            {
                // The record's own name where it has one, and this file's where
                // the refusal came from a half inside it: a defense record's
                // message says "defense record", which names nothing in a
                // folder of hundreds.
                _refusals.Add(
                    string.Equals(refused.Record, name, StringComparison.Ordinal)
                        ? refused.Message
                        : name + ": " + refused.Message);
            }
            catch (RetiredRecordException retired)
            {
                _refusals.Add(name + ": " + retired.Message);
            }
        }

        /// <summary>
        /// What one slot of one round's field met: the id of the stored round it
        /// drew, or nothing where the stand-in filled it.
        /// </summary>
        /// <param name="stage">Which round of the run, counted from one.</param>
        /// <param name="index">
        /// The index <see cref="FieldDraw.Drawn"/> holds for that slot, which is
        /// <see cref="FieldDraw.StoodIn"/> where nobody was stored for it.
        /// </param>
        public Hash64? Drawn(int stage, int index)
        {
            if (index == FieldDraw.StoodIn || stage < RoundRecord.FirstStage || stage > _ids.Count)
            {
                return null;
            }

            List<Hash64> stored = _ids[stage - 1];

            if (index < 0 || index >= stored.Count)
            {
                throw new SimulationException(
                    "A round's field is said to have drawn stored round "
                    + index.ToString(CultureInfo.InvariantCulture)
                    + " of the "
                    + stored.Count.ToString(CultureInfo.InvariantCulture)
                    + " at stage "
                    + stage.ToString(CultureInfo.InvariantCulture)
                    + ". A draw is an index inside its own stage's population, so one outside it is a "
                    + "draw and a pool that are about different stages.");
            }

            return stored[index];
        }

        /// <summary>
        /// The round, filed under its stage in id order, with empty stages
        /// opened in front of it.
        /// </summary>
        /// <remarks>
        /// Inserted in place rather than appended and sorted afterwards,
        /// because a stage is read as soon as the last record is offered and
        /// there is no moment between the two to sort in.
        /// </remarks>
        private void Take(Hash64 id, int stage, RoundOrders orders)
        {
            while (_stages.Count < stage)
            {
                _stages.Add(new List<RoundOrders>());
                _ids.Add(new List<Hash64>());
            }

            List<RoundOrders> members = _stages[stage - 1];
            List<Hash64> ids = _ids[stage - 1];
            int at = 0;

            while (at < ids.Count && ids[at].Value < id.Value)
            {
                at++;
            }

            members.Insert(at, orders);
            ids.Insert(at, id);
            Count++;
        }

    }
}
