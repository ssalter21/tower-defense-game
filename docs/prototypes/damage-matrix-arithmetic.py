"""Arithmetic probe for issue #75 — damage-type matrix width and armour formula.

Throwaway. It exists to turn a debate into a table, per the map's note that
question 4 is "a spreadsheet, not a debate".

The sim's arithmetic contract (ADR-0001, sim/Fix64.cs, sim.tests/Scan/BanTable.cs):

  * health and damage are plain `int`; there is no fixed-point on this path
  * `/` truncates toward zero, and that is a rule of the game
  * `System.Math` is banned, so no Max/Min/Clamp helper
  * `Dictionary` is banned, so a coefficient table is a flat array
  * content files reject a `.` before tokenising, so every constant is authored
    as the two integers it is made of

Run:  python docs/prototypes/damage-matrix-arithmetic.py
"""

# --------------------------------------------------------------------------
# The candidates
# --------------------------------------------------------------------------

# Matrix widths, as (best cell, worst cell) in percent.
WIDTHS = {
    "Legion TD 2   1.67:1": (125, 75),
    "Element TD 2     4:1": (200, 50),
    "Warcraft 3      40:1": (200, 5),
}

# Hit profiles that must both survive. The squad research is the reason: a
# five-archer squad and a cannon of the same nominal DPS must not diverge
# catastrophically just because one delivers it in five pieces.
PROFILES = {
    "5 archers x 5 dmg": (5, 5),
    "1 cannon  x 25 dmg": (1, 25),
    "3 mages   x 8 dmg": (3, 8),
    "1 siege   x 60 dmg": (1, 60),
}

ARMOURS = [0, 1, 2, 3, 5, 8, 12, 20]


# --------------------------------------------------------------------------
# The formulas, written exactly as integers would evaluate them
# --------------------------------------------------------------------------

def flat(dmg, mult, armour, floor=1):
    """Flat subtraction. The intuitive one. Ruled out — this quantifies why."""
    scaled = dmg * mult // 100
    out = scaled - armour
    return out if out > floor else floor


def two_step(dmg, mult, armour, k=1, floor=1):
    """Coefficient form, applied as two separate truncating steps."""
    scaled = dmg * mult // 100          # truncation #1
    out = scaled * 100 // (100 + k * armour)  # truncation #2
    return out if out > floor else floor


def fused(dmg, mult, armour, k=1, floor=1):
    """Coefficient form, algebraically collapsed. One multiply, one divide.

        dmg * mult / 100 * 100 / (100 + k*armour)  ==  dmg * mult / (100 + k*armour)

    True in algebra. In integers the fused form truncates ONCE instead of
    twice, so it is not merely tidier — it is a different (and strictly less
    lossy) function. Which is the whole of ADR-0001's warning.
    """
    out = dmg * mult // (100 + k * armour)
    return out if out > floor else floor


# --------------------------------------------------------------------------

def rule(title):
    print()
    print("=" * 78)
    print(title)
    print("=" * 78)


def check_fusion_differs():
    rule("1. two_step and fused are DIFFERENT functions in integers")
    disagreements = 0
    total = 0
    worst = None
    for dmg in range(1, 101):
        for mult in range(5, 201):
            for armour in range(0, 21):
                total += 1
                a = two_step(dmg, mult, armour, floor=0)
                b = fused(dmg, mult, armour, floor=0)
                if a != b:
                    disagreements += 1
                    if worst is None or (b - a) > worst[3]:
                        worst = (dmg, mult, armour, b - a)
    pct = 100 * disagreements / total
    print(f"  swept {total:,} (damage, multiplier, armour) triples")
    print(f"  they disagree on {disagreements:,} of them ({pct:.1f}%)")
    d, m, a, gap = worst
    print(f"  largest gap: dmg={d} mult={m}% armour={a} -> "
          f"two-step {two_step(d, m, a, floor=0)}, fused {fused(d, m, a, floor=0)} "
          f"(fused is +{gap})")
    print("  fused is never LOWER:",
          all(fused(d, m, a, floor=0) >= two_step(d, m, a, floor=0)
              for d in range(1, 101) for m in range(5, 201) for a in range(0, 21)))


def check_width_vs_volume():
    rule("2. What each width does to a small hit, BEFORE armour is involved")
    print("  A single hit of N damage through the worst cell of the matrix.")
    print("  Integer division, no floor. Zero means the type chart deleted the hit.\n")
    print(f"  {'width':<22} {'worst cell':>10} " +
          " ".join(f"{f'{n} dmg':>7}" for n in (2, 3, 5, 8, 15, 25, 60)))
    for name, (best, worst_cell) in WIDTHS.items():
        cells = " ".join(f"{n * worst_cell // 100:>7}" for n in (2, 3, 5, 8, 15, 25, 60))
        print(f"  {name:<22} {worst_cell:>9}% {cells}")
    print()
    print("  And the same hits through the BEST cell:\n")
    print(f"  {'width':<22} {'best cell':>10} " +
          " ".join(f"{f'{n} dmg':>7}" for n in (2, 3, 5, 8, 15, 25, 60)))
    for name, (best, worst_cell) in WIDTHS.items():
        cells = " ".join(f"{n * best // 100:>7}" for n in (2, 3, 5, 8, 15, 25, 60))
        print(f"  {name:<22} {best:>9}% {cells}")


def check_floor_collapses_wide():
    rule("3. A damage floor rescues small hits — and blinds a wide matrix")
    print("  With floor=1, how many DISTINCT outputs does the matrix produce")
    print("  for a hit of N damage? 1 means every cell in the table is the")
    print("  same cell, i.e. the type chart has stopped existing for that hit.\n")
    cells_narrow = list(range(75, 126, 5))
    cells_mid = list(range(50, 201, 10))
    cells_wide = [5, 10, 25, 50, 75, 100, 150, 200]
    print(f"  {'width':<22} " + " ".join(f"{f'{n} dmg':>7}" for n in (2, 3, 5, 8, 15, 25, 60)))
    for name, cells in (("Legion TD 2   1.67:1", cells_narrow),
                        ("Element TD 2     4:1", cells_mid),
                        ("Warcraft 3      40:1", cells_wide)):
        row = []
        for n in (2, 3, 5, 8, 15, 25, 60):
            outs = {max(1, n * c // 100) for c in cells}
            row.append(f"{len(outs):>7}")
        print(f"  {name:<22} " + " ".join(row))
    print()
    print("  Read the '2 dmg' and '3 dmg' columns. That is the quadratic")
    print("  punishment the squad research found, arriving through rounding")
    print("  rather than through the formula.")


def check_many_small_vs_few_big():
    rule("4. Many-small-hits vs few-big-hits, at equal nominal damage")
    print("  Every profile deals 25 nominal damage per volley. A formula is")
    print("  fair if the delivered totals stay together as armour rises.\n")
    for label, fn, kw in (("FLAT SUBTRACTION", flat, {}),
                          ("COEFFICIENT k=1 (fused)", fused, {"k": 1}),
                          ("COEFFICIENT k=6 (fused)", fused, {"k": 6})):
        print(f"  --- {label} ---")
        print(f"  {'profile':<20} " + " ".join(f"{f'a={a}':>6}" for a in ARMOURS))
        for pname, (count, each) in PROFILES.items():
            if count * each != 25 and count * each != 24 and count * each != 60:
                pass
            row = []
            for a in ARMOURS:
                row.append(f"{count * fn(each, 100, a, **kw):>6}")
            print(f"  {pname:<20} " + " ".join(row))
        print()
    print("  The 'archers' row under flat subtraction is the finding: five")
    print("  5-damage hits lose 5 damage per hit, i.e. 5x the armour, while")
    print("  the cannon loses it once.")


def check_effective_health_linear():
    rule("5. Effective health is linear in armour (the reason for the shape)")
    print("  A creep with 1000 base HP. Effective HP = HP * (100 + k*armour) / 100.")
    print("  Each point of armour should add exactly k% of BASE health.\n")
    base = 1000
    for k in (1, 6):
        print(f"  k={k}:")
        prev = None
        for a in range(0, 11):
            ehp = base * (100 + k * a) // 100
            delta = "" if prev is None else f"  (+{ehp - prev})"
            print(f"    armour {a:>2} -> effective {ehp:>6}{delta}")
            prev = ehp
        print()
    print("  Constant increments. Reduction diminishes, effective health does")
    print("  not — which is why armour can stack without a cap.")


def check_hard_counter_reachable():
    rule("6. Can a narrow matrix express #73's wave-9 HARD counter?")
    print("  #73 requires exactly one genuine hard-counter anchor. Ask what")
    print("  each width does to a creep with 1000 effective HP facing a tower")
    print("  that deals 25 damage a shot, in SHOTS TO KILL.\n")
    print(f"  {'width':<22} {'best cell':>10} {'worst cell':>11} {'ratio':>8}")
    for name, (best, worst_cell) in WIDTHS.items():
        good = 1000 // max(1, 25 * best // 100)
        bad = 1000 // max(1, 25 * worst_cell // 100)
        print(f"  {name:<22} {good:>7} shots {bad:>8} shots {bad / good:>7.2f}x")
    print()
    print("  1.67:1 is a tilt, not a counter. Widening the SCALAR layer to")
    print("  reach a hard counter is what breaks volume (sections 2 and 3).")
    print("  A capability GATE is binary and costs the scalar layer nothing:")
    print("  shots-to-kill goes to infinity without any cell leaving 75-125.")


def check_resolution_at_shipped_scale():
    rule("8. Resolution is bought with the SIZE of the numbers")
    print("  content/units.txt today: bolt 9-15, mortar 21-34 damage;")
    print("  grunt 200 HP, runner 110 HP. Ask what a 75-125 matrix in 5%")
    print("  steps (11 cells) can actually SAY at that scale.\n")
    cells = list(range(75, 126, 5))
    print(f"  {'hit':>6} {'distinct outputs of 11 cells':>30}   {'outputs'}")
    for scale, tag in ((1, "shipped"), (10, "x10")):
        print(f"  --- {tag} ---")
        for hit in (9, 15, 21, 34):
            h = hit * scale
            outs = sorted({h * c // 100 for c in cells})
            print(f"  {h:>6} {len(outs):>30}   {outs if len(outs) <= 12 else '...'}")

    print()
    print("  And the same question for ARMOUR: how many distinct results does")
    print("  armour 0..20 produce against one hit?\n")
    print(f"  {'hit':>6} {'k=1':>6} {'k=6':>6}")
    for scale, tag in ((1, "shipped"), (10, "x10")):
        print(f"  --- {tag} ---")
        for hit in (9, 15, 21, 34):
            h = hit * scale
            n1 = len({fused(h, 100, a, k=1, floor=1) for a in range(21)})
            n6 = len({fused(h, 100, a, k=6, floor=1) for a in range(21)})
            print(f"  {h:>6} {n1:>6} {n6:>6}")
    print()
    print("  At the shipped scale, k=1 armour is nearly BINARY -- most of the")
    print("  0..20 range collapses to one number. k=6 discriminates, at the")
    print("  cost of being three times as violent at armour 20. Multiplying")
    print("  every damage and HP number by ten buys back the resolution")
    print("  without touching either formula.")


def check_k1_plateau():
    rule("9. The k=1 plateau, spelled out")
    print("  A 9-damage bolt, coefficient k=1, armour 0..20:\n")
    print("    armour: " + " ".join(f"{a:>3}" for a in range(21)))
    print("    dealt : " + " ".join(f"{fused(9, 100, a, k=1):>3}" for a in range(21)))
    print("    k=6   : " + " ".join(f"{fused(9, 100, a, k=6):>3}" for a in range(21)))
    print()
    print("  Under k=1 the bolt deals 8 for eleven consecutive points of")
    print("  armour. That is armour that a player cannot feel and a balance")
    print("  sweep cannot tune.")


def check_no_overflow():
    rule("7. Overflow headroom (Fix64 throws; int must not silently wrap)")
    worst = 100000 * 200
    print(f"  worst intermediate = maxDamage * maxCell = 100000 * 200 = {worst:,}")
    print(f"  int.MaxValue = {2**31 - 1:,}")
    print(f"  headroom = {(2**31 - 1) // worst:,}x")
    print("  Safe with two orders of magnitude to spare on plain int.")


# --------------------------------------------------------------------------
# THE DECIDED RULESET (issue #75, 7 August 2026) and its validation
# --------------------------------------------------------------------------

# 3 attack types x 3 armour types, a Latin square. Every row and every column
# carries exactly one 140, one 100 and one 70, so no type is globally better
# and every cell is reachable.
CYCLE = {
    #            Swift  Armoured  Arcane
    "Pierce": (140, 70, 100),
    "Impact": (70, 100, 140),
    "Magic": (100, 140, 70),
}
ARMOUR_NAMES = ("Swift", "Armoured", "Arcane")

FLOOR = 1


def dealt(base, bonus, cell, armour):
    """The decided formula. One multiply, one divide, one truncation.

    Armour is authored as percent of base effective health added per point,
    so k is 1 and disappears from the expression entirely.
    """
    out = (base + bonus) * cell // (100 + armour)
    return out if out > FLOOR else FLOOR


def check_decided_ruleset():
    rule("10. THE DECIDED RULESET -- validation")
    print("  matrix   3x3 Latin square, cells in {70, 100, 140}, spread 2:1")
    print("  armour   dealt = (base + bonus) * cell / (100 + armour)")
    print("           k folded to 1; armour reads as % of base EHP per point")
    print("  floor    1")
    print("  scale    every damage and HP number x10")
    print()

    print("  --- the table is a Latin square ---")
    print(f"    {'':<8}" + "".join(f"{n:>10}" for n in ARMOUR_NAMES))
    for atk, row in CYCLE.items():
        print(f"    {atk:<8}" + "".join(f"{v:>9}%" for v in row))
    rows_ok = all(sorted(r) == [70, 100, 140] for r in CYCLE.values())
    cols_ok = all(sorted(CYCLE[a][i] for a in CYCLE) == [70, 100, 140]
                  for i in range(3))
    print(f"    every row is a permutation of (70,100,140): {rows_ok}")
    print(f"    every column is a permutation of (70,100,140): {cols_ok}")
    print(f"    spread best:worst = {140 / 70:.2f}:1")
    print(f"    distinct cell values = {len({v for r in CYCLE.values() for v in r})}"
          f" of 9 positions -- every cell reachable")

    print()
    print("  --- nothing rounds to the floor at the x10 scale ---")
    # shipped damage x10, plus headroom either side
    bases = list(range(60, 401, 10))
    bonuses = [0, 90, 180, 270, 360]
    armours = list(range(0, 201, 5))
    hit_floor = 0
    lowest = None
    total = 0
    for b in bases:
        for bo in bonuses:
            for a in armours:
                for cell in (70, 100, 140):
                    total += 1
                    d = dealt(b, bo, cell, a)
                    if d <= FLOOR:
                        hit_floor += 1
                    if lowest is None or d < lowest[0]:
                        lowest = (d, b, bo, cell, a)
    print(f"    swept {total:,} (base, bonus, cell, armour) combinations")
    print(f"    hit the floor: {hit_floor}")
    d, b, bo, cell, a = lowest
    print(f"    lowest result: {d}  (base={b} bonus={bo} cell={cell}% armour={a})")

    print()
    print("  --- armour still discriminates across its whole range ---")
    for b in (90, 150, 340):
        n = len({dealt(b, 0, 100, a) for a in range(0, 101)})
        print(f"    base {b:>3}, armour 0..100 -> {n} distinct results of 101")

    print()
    print("  --- the counter is a steep gradient, not a wall ---")
    print("    wave-9 anchor: 2000 EHP, armour 60, tagged.")
    anchor_hp, anchor_armour = 2000, 60
    for bonus, label in ((0, "no counter"), (180, "+180 bonus"), (270, "+270 bonus")):
        per_shot = dealt(90, bonus, 100, anchor_armour)
        shots = -(-anchor_hp // per_shot)
        print(f"    {label:<12} {per_shot:>4} per shot, {shots:>4} shots to kill")
    base_shots = -(-anchor_hp // dealt(90, 0, 100, anchor_armour))
    ctr_shots = -(-anchor_hp // dealt(90, 270, 100, anchor_armour))
    print(f"    ratio {base_shots / ctr_shots:.2f}x -- prepared beats unprepared")
    print("    by a wide margin, and unprepared still finishes. Which is the")
    print("    softening of #73's 'hard counter' that #75 chose deliberately.")

    print()
    print("  --- many-small vs few-big survives the final formula ---")
    print(f"    {'profile':<22} " + " ".join(f"{f'a={a}':>6}" for a in (0, 20, 60, 120, 200)))
    for pname, (count, each) in (("5 archers x 50", (5, 50)),
                                 ("1 cannon x 250", (1, 250)),
                                 ("3 mages x 80", (3, 80)),
                                 ("1 siege x 600", (1, 600))):
        row = " ".join(f"{count * dealt(each, 0, 100, a):>6}" for a in (0, 20, 60, 120, 200))
        print(f"    {pname:<22} {row}")
    print("    The volley and the cannon fall off together. No quadratic.")

    print()
    print("  --- overflow headroom on plain int ---")
    worst = (400 + 400) * 140
    print(f"    worst intermediate (base+bonus)*cell = {worst:,}")
    print(f"    int.MaxValue = {2**31 - 1:,}  ->  {(2**31 - 1) // worst:,}x headroom")


if __name__ == "__main__":
    check_fusion_differs()
    check_width_vs_volume()
    check_floor_collapses_wide()
    check_many_small_vs_few_big()
    check_effective_health_linear()
    check_hard_counter_reachable()
    check_resolution_at_shipped_scale()
    check_k1_plateau()
    check_no_overflow()
    check_decided_ruleset()
    print()
