using System;
using UnityEngine;

namespace View
{
    /// <summary>
    /// Which tick is on screen, and the only thing in this client that decides
    /// it: <see cref="Advance"/> to play, <see cref="SeekTo"/> to jump.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Two operations, because there are exactly two things that can happen
    /// to a playback head, and they want opposite treatment.</b> Advancing is
    /// continuous — however many ticks a frame covers, every one of them
    /// happened, in order, with the match watching. Seeking is a
    /// discontinuity: the ticks between where you were and where you are now
    /// did not happen on screen.
    /// </para>
    /// <para>
    /// <b>The discontinuity is signalled rather than inferred.</b> Nothing here
    /// looks at how far the tick moved and decides. It cannot: a fast-forward
    /// at eight times speed crosses eight ticks in a frame quite legitimately,
    /// and a tick-delta heuristic would clear the effects every frame of it.
    /// So <see cref="SeekTo"/> is the signal, and the only signal.
    /// </para>
    /// <para>
    /// <b>Speed multiplies the clock and nothing else.</b> Everything
    /// downstream — how far a creep gets, how fast its legs go, how long a
    /// tracer lasts — is already a function of ticks rather than of seconds, so
    /// fast-forward is correct here or it is correct nowhere. That is not a
    /// coincidence; it is why decoration ages on the tick and why locomotion
    /// phase comes from distance travelled.
    /// </para>
    /// <para>
    /// This is a plain object rather than a component, so the seam can be
    /// driven a frame at a time by a test with no update loop involved. The
    /// component that owns one is <see cref="PlaybackControls"/>, and it does
    /// nothing but hand this class <c>Time.deltaTime</c> and the slider's
    /// value.
    /// </para>
    /// </remarks>
    public sealed class PlaybackController
    {
        /// <summary>
        /// The most ticks one frame may advance the match by, at normal speed.
        /// </summary>
        /// <remarks>
        /// A frame that arrives late — a domain reload, a breakpoint, a
        /// minimised editor — would otherwise try to catch up the whole gap at
        /// once and stall for longer than the gap. Capping means the match runs
        /// slow for a moment instead, which is visible and recoverable, rather
        /// than freezing, which looks like a hang. The cap scales with
        /// <see cref="Speed"/>, because at eight times speed eight ticks in a
        /// frame is the job rather than a backlog.
        /// </remarks>
        public const int MaxTicksPerFrame = 8;

        /// <summary>The fastest the match may be watched at.</summary>
        public const float FastestSpeed = 8f;

        private readonly MatchView _view;

        private float _tickClock;

        private float _speed = 1f;

        /// <summary>Drives <paramref name="view"/>, which must already be running.</summary>
        public PlaybackController(MatchView view)
        {
            _view = view != null
                ? view
                : throw new ArgumentNullException(nameof(view));

            if (!view.IsRunning)
            {
                throw new ArgumentException(
                    "The view has no match to play. Begin one before building a playback controller for "
                    + "it, so there is never a controller whose tick means nothing.",
                    nameof(view));
            }
        }

        /// <summary>The match being played.</summary>
        public MatchView View => _view;

        /// <summary>
        /// How many times faster than real time the match runs.
        /// </summary>
        /// <remarks>
        /// Throws rather than clamps. A clamped speed is a control that looks
        /// like it worked and did not, and the two ways to get one wrong are
        /// both worth hearing about: zero or less is a match that never
        /// advances again — running backwards is re-simulating from the
        /// beginning, which is <see cref="SeekTo"/> and not a negative speed —
        /// and past the ceiling is a frame doing more re-simulation than it can
        /// afford.
        /// </remarks>
        public float Speed
        {
            get => _speed;

            set
            {
                if (value <= 0f || value > FastestSpeed)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(value),
                        value,
                        "A watching speed has to be above zero and no more than " + FastestSpeed + ".");
                }

                _speed = value;
            }
        }

        /// <summary>Whether the clock is stopped. A paused match still draws.</summary>
        public bool IsPaused { get; set; }

        /// <summary>The tick on screen.</summary>
        public int Tick => _view.Current.Tick;

        /// <summary>The tick the match ends on — the far end of the scrub bar.</summary>
        public int FinalTick => _view.FinalTick;

        /// <summary>Whether the match will advance no further.</summary>
        public bool IsFinished => _view.IsFinished;

        /// <summary>
        /// Plays <paramref name="deltaSeconds"/> of wall clock, then draws.
        /// </summary>
        /// <remarks>
        /// The leftover fraction of a tick is the interpolation alpha, which is
        /// what makes this a clock that decides <i>when to advance the
        /// simulation</i> rather than one that decides what a frame looks like.
        /// Drawing the same alpha twice draws the same picture.
        /// </remarks>
        public void Advance(float deltaSeconds)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(deltaSeconds),
                    "Playback runs forwards. Going back to a tick is re-simulating from the beginning, "
                    + "which is SeekTo -- a negative frame is a seek nobody signalled.");
            }

            float tickSeconds = 1f / Sim.Match.TicksPerSecond;

            if (!IsPaused && !_view.IsFinished)
            {
                _tickClock += deltaSeconds * _speed;

                int cap = Mathf.Max(1, Mathf.CeilToInt(MaxTicksPerFrame * _speed));
                int stepped = 0;

                while (_tickClock >= tickSeconds && !_view.IsFinished && stepped < cap)
                {
                    _view.StepOneTick();
                    _tickClock -= tickSeconds;
                    stepped++;
                }
            }

            if (_view.IsFinished)
            {
                _tickClock = 0f;
            }

            _view.Draw(_view.IsFinished ? 1f : _tickClock / tickSeconds);
        }

        /// <summary>
        /// Puts the match on <paramref name="tick"/>, however far away it is and
        /// in whichever direction.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is the discontinuity, and being called at all is the whole of
        /// how it is signalled. What it costs and why it is worth it are on
        /// <see cref="MatchView.ReSimulateTo"/>, which does the work.
        /// </para>
        /// <para>
        /// Seeking past the end lands on the end: the simulation stops
        /// advancing once it is finished, so jumping to the end is this call
        /// with a large number and needs no second code path.
        /// </para>
        /// </remarks>
        public void SeekTo(int tick)
        {
            _view.ReSimulateTo(tick);

            // The partial tick belonged to where playback was, not to where it
            // now is. Carrying it over would put the first frame after a seek
            // part-way between two ticks nobody asked for.
            _tickClock = 0f;
        }

        /// <summary>
        /// Resolves the match: seek to the last tick of it. Named because
        /// "resolve this instantly" is a thing people ask for by name.
        /// </summary>
        public void SeekToEnd() => SeekTo(FinalTick);
    }
}
