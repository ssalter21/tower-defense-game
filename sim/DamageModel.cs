using System;
using System.Collections.Generic;
using System.Globalization;

namespace Sim
{
    /// <summary>
    /// One stage of the stat pipeline: a named transformation a rolled damage
    /// number passes through on its way to being dealt.
    /// </summary>
    /// <remarks>
    /// Every stage is one integer division, and an integer division truncates.
    /// Two stages therefore truncate twice and compute a different function
    /// from the same algebra written as one.
    /// </remarks>
    public enum StatStage
    {
        /// <summary>
        /// The type chart and the target's armour, applied together as one
        /// multiply and one divide.
        /// </summary>
        TypedMitigation = 0,
    }

    /// <summary>
    /// How much damage a shot actually deals: the stat pipeline, and the one
    /// expression it is made of.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The pipeline is <see cref="Stages"/>, and it has one stage.</b>
    /// <see cref="Dealt"/> walks that list and applies nothing that is not on
    /// it, so a stage declared in <see cref="StatStage"/> and left off the list
    /// changes no damage number anywhere -- and the build gate says so.
    /// </para>
    /// <para>
    /// <b>The one stage is one multiply and one divide:</b>
    /// </para>
    /// <code>
    /// dealt = (base + bonusVsTag) * cell / (denominator + coefficient * armour)
    /// if (dealt &lt; floor) dealt = floor;
    /// </code>
    /// <para>
    /// The counter joins the hit before the multiply, so a high-armour target
    /// blunts the bonus built to kill it along with everything else. The floor
    /// is applied after the pipeline rather than inside a stage, because it is a
    /// guarantee about the amount dealt and not a transformation of it.
    /// </para>
    /// <para>
    /// <b>The intermediate is a <c>long</c>.</b> The product of a hit and a cell
    /// is the one place in this expression that leaves the range of an
    /// <c>int</c>, and a wrapped multiply deals a negative amount that heals its
    /// target. A cell is bounded where the ruleset is parsed, so the product
    /// cannot leave a <c>long</c>; a dealt amount that will not fit back into an
    /// <c>int</c> is a throw.
    /// </para>
    /// </remarks>
    public static class DamageModel
    {
        /// <summary>The stages, in the order they are applied.</summary>
        private static readonly StatStage[] StageOrder = { StatStage.TypedMitigation };

        /// <summary>
        /// The stat pipeline: every stage a rolled damage number passes
        /// through, in order. Applying a stage means being on this list.
        /// </summary>
        public static IReadOnlyList<StatStage> Stages => StageOrder;

        /// <summary>What a stage is called, in a message.</summary>
        public static string NameOf(StatStage stage)
        {
            switch (stage)
            {
                case StatStage.TypedMitigation:
                    return "typed mitigation";

                default:
                    throw NoSuchStage(stage);
            }
        }

        /// <summary>
        /// What a shot of <paramref name="baseDamage"/> deals to a target of
        /// this armour type carrying this many points of armour.
        /// </summary>
        /// <param name="rules">The matrix, the armour expression's shape and the floor.</param>
        /// <param name="baseDamage">The rolled damage, before anything is done to it.</param>
        /// <param name="bonusVsTag">The counter's addition to the base. Zero when nothing is countered.</param>
        /// <param name="attack">The attacker's attack type. A row of the matrix.</param>
        /// <param name="armour">The target's armour type. A column of the matrix.</param>
        /// <param name="armourPoints">The target's armour, in percent of its base effective health per point.</param>
        public static int Dealt(
            Ruleset rules,
            int baseDamage,
            int bonusVsTag,
            AttackType attack,
            ArmourType armour,
            int armourPoints)
        {
            if (rules is null)
            {
                throw new ArgumentNullException(nameof(rules));
            }

            if (baseDamage < 0)
            {
                throw new SimulationException(
                    "A shot rolled "
                    + baseDamage.ToString(CultureInfo.InvariantCulture)
                    + " damage. A hit is never negative: the floor guarantees a minimum and a negative "
                    + "base would arrive underneath it.");
            }

            if (bonusVsTag < 0)
            {
                throw new SimulationException(
                    "A shot carries a bonus of "
                    + bonusVsTag.ToString(CultureInfo.InvariantCulture)
                    + " against its target's tag. A counter adds to the base and never subtracts from it; "
                    + "a weakness is authored as the matrix cell it is.");
            }

            if (armourPoints < 0)
            {
                throw new SimulationException(
                    "A target carries "
                    + armourPoints.ToString(CultureInfo.InvariantCulture)
                    + " points of armour. Armour is percent of base effective health added per point, so "
                    + "a negative value is a target that takes more damage the more armoured it is.");
            }

            // The counter joins the hit here, before typing and mitigation.
            long amount = (long)baseDamage + bonusVsTag;

            for (int index = 0; index < StageOrder.Length; index++)
            {
                amount = Apply(StageOrder[index], rules, amount, attack, armour, armourPoints);
            }

            if (amount < rules.DamageFloor)
            {
                amount = rules.DamageFloor;
            }

            return (int)amount;
        }

        /// <summary>One stage of the pipeline, applied to the running amount.</summary>
        private static long Apply(
            StatStage stage,
            Ruleset rules,
            long amount,
            AttackType attack,
            ArmourType armour,
            int armourPoints)
        {
            switch (stage)
            {
                case StatStage.TypedMitigation:
                    return Mitigate(rules, amount, attack, armour, armourPoints);

                default:
                    throw NoSuchStage(stage);
            }
        }

        /// <summary>
        /// The fused expression: one multiply by the matrix cell, one divide by
        /// the armour denominator, one truncation.
        /// </summary>
        private static long Mitigate(
            Ruleset rules,
            long amount,
            AttackType attack,
            ArmourType armour,
            int armourPoints)
        {
            long denominator = rules.ArmourDenominator + ((long)rules.ArmourPercentPerPoint * armourPoints);

            if (denominator < 1)
            {
                throw new SimulationException(
                    "The armour expression's denominator came out at "
                    + denominator.ToString(CultureInfo.InvariantCulture)
                    + " against "
                    + armourPoints.ToString(CultureInfo.InvariantCulture)
                    + " points of armour. It is the divisor of every hit in the game and it is never "
                    + "less than one.");
            }

            // One multiply, one divide, one truncation.
            long dealt = (amount * rules.Matrix.Cell(attack, armour)) / denominator;

            if (dealt > int.MaxValue)
            {
                throw new SimulationException(
                    "A hit resolved to "
                    + dealt.ToString(CultureInfo.InvariantCulture)
                    + ", which does not fit in the 32-bit integer a health pool is kept in.");
            }

            return dealt;
        }

        private static SimulationException NoSuchStage(StatStage stage) =>
            new SimulationException(
                "Stat stage "
                + ((int)stage).ToString(CultureInfo.InvariantCulture)
                + " is not one this pipeline knows how to apply. A stage is declared, listed in the "
                + "pipeline and given a branch here, and all three or none.");
    }
}
