namespace CyberRogueWars.Core
{
    /// <summary>
    /// Holds the cumulative stat bonuses a player has drafted for one specific unit type.
    /// Stored in <see cref="PlayerData.UnitBoosts"/>, keyed by <see cref="CyberRogueWars.UnitType"/>.
    ///
    /// Why a separate class instead of a struct or tuple?
    /// Because we want reference semantics — PlayerData.GetBoosts() can return the
    /// same object and mutations stick. No accidental copy-and-discard bugs! 🔒
    /// </summary>
    public class UnitStatModifiers
    {
        // ── Offensive ────────────────────────────────────────────────────────
        /// <summary>Flat bonus added to AttackPower when calculating EffectiveAttack.</summary>
        public int AttackBonus    { get; set; } = 0;

        // ── Defensive ────────────────────────────────────────────────────────
        /// <summary>Flat bonus added to Defense when calculating EffectiveDefense.</summary>
        public int DefenseBonus   { get; set; } = 0;

        /// <summary>Flat bonus added to MaxHealth (applied at spawn time).</summary>
        public int MaxHealthBonus { get; set; } = 0;

        // ── Mobility ─────────────────────────────────────────────────────────
        /// <summary>Flat bonus added to MoveRange when calculating EffectiveMovement.</summary>
        public int MovementBonus  { get; set; } = 0;

        // ── Helpers ──────────────────────────────────────────────────────────

        /// <summary>Returns true if any bonus is non-zero — handy for UI "show upgrades" checks.</summary>
        public bool HasAnyBonus()
        {
            return AttackBonus != 0
                || DefenseBonus != 0
                || MaxHealthBonus != 0
                || MovementBonus != 0;
        }

        public override string ToString()
        {
            return $"ATK+{AttackBonus} DEF+{DefenseBonus} HP+{MaxHealthBonus} MOV+{MovementBonus}";
        }
    }
}