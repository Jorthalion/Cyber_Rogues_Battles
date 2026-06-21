using Godot;
using Godot.Collections;
using System.Collections.Generic;
using CyberRogueWars.Core;

namespace CyberRogueWars
{
    /// <summary>
    /// Tracks everything about one player during a match.
    /// Lives in memory only — not a saved Resource.
    /// </summary>
    public class PlayerData
    {
        // ─── Identity ─────────────────────────────────────────────
        public string PlayerName  { get; set; } = "Player";
        public FactionType Faction { get; set; } = FactionType.Confederation;
        public bool IsHuman       { get; set; } = true;

        // ─── Turn Order ───────────────────────────────────────────
        /// <summary>
        /// Current position in the turn order (0 = goes first).
        /// Rotates each round: last player becomes first next round.
        /// </summary>
        public int TurnOrderIndex { get; set; } = 0;

        // ─── Card Hand ────────────────────────────────────────────
        /// <summary>Cards currently in this player's hand.</summary>
        public List<CardData> Hand { get; private set; } = new();

        /// <summary>The card chosen during the Planning Phase. Null until chosen.</summary>
        public CardData ChosenCard { get; set; } = null;

        // ─── Units ────────────────────────────────────────────────
        /// <summary>All units this player has on the board right now.</summary>
        public List<UnitInstance> ActiveUnits { get; private set; } = new();

        // ─── Resources / Economy ──────────────────────────────────
        /// <summary>
        /// Supply points — spent to summon units via Summon cards.
        /// Replenishes a small amount each turn.
        /// </summary>
        public int Supply     { get; set; } = 10;
        public int MaxSupply  { get; set; } = 20;

        // ─── Scoring ──────────────────────────────────────────────
        public int Score { get; set; } = 0;

        // ─── Commander ────────────────────────────────────────────
        /// <summary>
        /// The player's commander unit (special unit — if it dies, player loses).
        /// May be null before the match starts.
        /// </summary>
        public UnitInstance Commander { get; set; } = null;

        // ─── Upgrade Tracking ─────────────────────────────────────
        /// <summary>
        /// Unit types this player has unlocked via the upgrade draft.
        /// Unlocked units can be summoned from Summon cards.
        /// </summary>
        public HashSet<UnitType> UnlockedUnits { get; private set; } = new();

        /// <summary>
        /// Cumulative stat bonuses per unit type, accumulated through upgrade drafts.
        /// Key = UnitType, Value = modifiers to apply when that unit is spawned.
        /// </summary>
        public Dictionary<UnitType, UnitStatModifiers> UnitBoosts { get; private set; } = new();

        /// <summary>
        /// Returns the stat modifiers for a given unit type.
        /// If none exist yet, creates an empty entry so callers can write to it safely.
        /// Think of it like opening a new treasure chest — it starts empty but it's ready! 🪙
        /// </summary>
        public UnitStatModifiers GetBoosts(UnitType unitType)
        {
            if (!UnitBoosts.TryGetValue(unitType, out var mods))
            {
                mods = new UnitStatModifiers();
                UnitBoosts[unitType] = mods;
            }
            return mods;
        }

        // ─── Helpers ──────────────────────────────────────────────

        /// <summary>True if the commander is alive and on the board.</summary>
        public bool IsAlive => Commander != null && Commander.CurrentHealth > 0;

        /// <summary>Add a card to hand (called during draft or turn start).</summary>
        public void AddCard(CardData card)
        {
            if (card != null) Hand.Add(card);
        }

        /// <summary>Remove a card from hand (called after it's played).</summary>
        public void PlayCard(CardData card)
        {
            Hand.Remove(card);
            ChosenCard = null;
        }

        /// <summary>Register a unit as belonging to this player.</summary>
        public void AddUnit(UnitInstance unit)
        {
            if (unit != null && !ActiveUnits.Contains(unit))
                ActiveUnits.Add(unit);
        }

        /// <summary>Remove a dead or retreated unit from the roster.</summary>
        public void RemoveUnit(UnitInstance unit)
        {
            ActiveUnits.Remove(unit);
        }

        /// <summary>Replenish supply at end of turn (small trickle).</summary>
        public void TickSupply(int amount = 2)
        {
            Supply = System.Math.Min(Supply + amount, MaxSupply);
        }
    }
}