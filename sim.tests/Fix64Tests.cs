using Sim;

namespace Sim.Tests;

/// <summary>
/// Q32.32 against known inputs and known outputs.
/// </summary>
/// <remarks>
/// Every expected value here is written as an exact integer, worked out from
/// the definition <c>value = raw / 2^32</c> rather than read off a run. A test
/// whose expectations came from the implementation is a test that only ever
/// says "it still does what it did", which is not what this arithmetic needs
/// to promise. What it needs to promise is that a replay from 2029 produces
/// the same numbers, and that is a claim about specific values.
/// </remarks>
public class Fix64Tests
{
    private const long OneRaw = 4294967296L; // 2^32

    [Fact]
    public void One_is_two_to_the_thirty_second()
    {
        Assert.Equal(OneRaw, Fix64.One.Raw);
        Assert.Equal(32, Fix64.FractionalBits);
    }

    [Fact]
    public void Whole_numbers_convert_exactly_across_the_whole_int_range()
    {
        Assert.Equal(0L, Fix64.FromInt(0).Raw);
        Assert.Equal(OneRaw, Fix64.FromInt(1).Raw);
        Assert.Equal(-7L * OneRaw, Fix64.FromInt(-7).Raw);

        // The extremes are the interesting ones: int.MinValue scales to
        // exactly long.MinValue, so the conversion is total.
        Assert.Equal(long.MinValue, Fix64.FromInt(int.MinValue).Raw);
        Assert.Equal(9223372032559808512L, Fix64.FromInt(int.MaxValue).Raw);
    }

    [Fact]
    public void A_half_is_exact_and_a_third_is_the_nearest_representable_value_below()
    {
        Assert.Equal(2147483648L, Fix64.FromRatio(1, 2).Raw);
        Assert.Equal(1073741824L, Fix64.FromRatio(1, 4).Raw);

        // 2^32 / 3 = 1431655765.333..., and rounding is toward zero.
        Assert.Equal(1431655765L, Fix64.FromRatio(1, 3).Raw);
        Assert.Equal(-1431655765L, Fix64.FromRatio(-1, 3).Raw);
    }

    [Fact]
    public void Addition_and_subtraction_are_exact()
    {
        Assert.Equal(Fix64.FromInt(7), Fix64.FromInt(3) + Fix64.FromInt(4));
        Assert.Equal(Fix64.FromInt(-1), Fix64.FromInt(3) - Fix64.FromInt(4));
        Assert.Equal(Fix64.FromRatio(1, 2), Fix64.FromRatio(1, 4) + Fix64.FromRatio(1, 4));
    }

    [Fact]
    public void Multiplication_keeps_every_bit_the_type_can_hold()
    {
        Assert.Equal(Fix64.FromInt(12), Fix64.FromInt(3) * Fix64.FromInt(4));
        Assert.Equal(Fix64.FromInt(25000), Fix64.FromInt(100000) * Fix64.FromRatio(1, 4));
        Assert.Equal(Fix64.FromRatio(1, 4), Fix64.FromRatio(1, 2) * Fix64.FromRatio(1, 2));

        // The 128-bit intermediate is what makes this exact: the raw product
        // of two halves is 2^63, which does not fit in a long, and a
        // 64-bit-only implementation would wrap here.
        Assert.Equal(Fix64.One, Fix64.FromRatio(1, 2) * Fix64.FromInt(2));
    }

    [Fact]
    public void Multiplication_truncates_toward_zero_in_both_directions()
    {
        // A third is 1431655765 raw, so three of them is 4294967295 -- one
        // ulp short of one, and short on the negative side too rather than
        // rounding away from zero.
        Assert.Equal(4294967295L, (Fix64.FromRatio(1, 3) * Fix64.FromInt(3)).Raw);
        Assert.Equal(-4294967295L, (Fix64.FromRatio(-1, 3) * Fix64.FromInt(3)).Raw);
    }

    [Fact]
    public void Division_is_the_inverse_of_multiplication_to_the_last_bit()
    {
        Assert.Equal(Fix64.FromInt(4), Fix64.FromInt(12) / Fix64.FromInt(3));
        Assert.Equal(1431655765L, (Fix64.One / Fix64.FromInt(3)).Raw);
        Assert.Equal(-1431655765L, (Fix64.One / Fix64.FromInt(-3)).Raw);

        // A divisor above 2^32 exercises the 128-bit path rather than the
        // shortcut a 64-bit implementation would be tempted to take.
        Assert.Equal(Fix64.FromRatio(1, 8), Fix64.FromInt(1000000) / Fix64.FromInt(8000000));
    }

    [Fact]
    public void Overflow_throws_rather_than_wrapping()
    {
        Assert.Throws<OverflowException>(() => Fix64.MaxValue + Fix64.Epsilon);
        Assert.Throws<OverflowException>(() => Fix64.MinValue - Fix64.Epsilon);
        Assert.Throws<OverflowException>(() => -Fix64.MinValue);
        Assert.Throws<OverflowException>(() => Fix64.MinValue.Abs());
        Assert.Throws<OverflowException>(() => Fix64.FromInt(1000000) * Fix64.FromInt(1000000));
        Assert.Throws<OverflowException>(() => Fix64.MaxValue / Fix64.Epsilon);
    }

    [Fact]
    public void Dividing_by_zero_throws()
    {
        Assert.Throws<DivideByZeroException>(() => Fix64.One / Fix64.Zero);
    }

    [Fact]
    public void Truncation_and_flooring_differ_on_negatives_and_say_so()
    {
        Fix64 minusThreeAndAHalf = Fix64.FromRatio(-7, 2);

        Assert.Equal(-3, minusThreeAndAHalf.ToIntTowardZero());
        Assert.Equal(-4, minusThreeAndAHalf.ToIntFloor());
        Assert.Equal(Fix64.FromRatio(1, 2), minusThreeAndAHalf.Fraction());

        Assert.Equal(3, Fix64.FromRatio(7, 2).ToIntTowardZero());
        Assert.Equal(3, Fix64.FromRatio(7, 2).ToIntFloor());
    }

    [Fact]
    public void Comparison_and_equality_follow_the_raw_value()
    {
        Assert.True(Fix64.FromInt(1) < Fix64.FromInt(2));
        Assert.True(Fix64.FromInt(-2) < Fix64.FromInt(-1));
        Assert.True(Fix64.FromRatio(1, 3) != Fix64.FromRatio(1, 4));
        Assert.Equal(Fix64.FromInt(2), Fix64.Max(Fix64.FromInt(1), Fix64.FromInt(2)));
        Assert.Equal(Fix64.FromInt(1), Fix64.Min(Fix64.FromInt(1), Fix64.FromInt(2)));
        Assert.Equal(-1, Fix64.FromInt(-5).Sign());
        Assert.Equal(0, Fix64.Zero.Sign());
    }

    [Fact]
    public void Rendering_is_decimal_and_never_goes_near_a_float()
    {
        Assert.Equal("0.333333333", Fix64.FromRatio(1, 3).ToString());
        Assert.Equal("-0.333333333", Fix64.FromRatio(-1, 3).ToString());
        Assert.Equal("2.000000000", Fix64.FromInt(2).ToString());
        Assert.Equal("-3.500000000", Fix64.FromRatio(-7, 2).ToString());
        Assert.Equal("0.000000000", Fix64.Epsilon.ToString());
    }
}
