using Godot;

namespace CyberRogueWars.Core
{
    /// <summary>
    /// UnitData is a blueprint resource — it describes what a unit TYPE is.
    /// Think of it like a trading card: stats, faction, role. Immutable at runtime.
    /// To make a live unit on the board, see UnitInstance.cs.
    /// </summary>
    [GlobalClass]
    public partial class UnitData : Resource
    {
        // ── Identity ──────────────────────────────────────────────────────────
        [Export] public string UnitName        { get; set; } = "Unknown Unit";
        [Export] public FactionType Faction    { get; set; } = FactionType.None;
        [Export] public UnitType UnitType      { get; set; } = UnitType.Sludger;
        [Export] public UnitRole Role          { get; set; } = UnitRole.Infantry;

        // ── Core Stats ────────────────────────────────────────────────────────
        [Export] public int MaxHealth          { get; set; } = 10;
        [Export] public int AttackPower        { get; set; } = 3;
        [Export] public int Defense            { get; set; } = 1;
        [Export] public int MoveRange          { get; set; } = 2;   // tiles per Unit Phase
        [Export] public int AttackRange        { get; set; } = 1;   // 1 = melee, 2+ = ranged

        // ── Cost ─────────────────────────────────────────────────────────────
        [Export] public int EnergyCost         { get; set; } = 1;   // energy to summon

        // ── Visuals ───────────────────────────────────────────────────────────
        [Export] public Texture2D Sprite       { get; set; } = null;
        [Export] public string Description     { get; set; } = "";

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>
        /// Quick sanity check — returns true if this data looks usable.
        /// A goblin always checks her tools before the job!
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(UnitName)
                && MaxHealth > 0
                && MoveRange >= 0;
        }

        public override string ToString()
        {
            return $"[{Faction}] {UnitName} | HP:{MaxHealth} ATK:{AttackPower} DEF:{Defense} MOV:{MoveRange}";
        }
    }
}