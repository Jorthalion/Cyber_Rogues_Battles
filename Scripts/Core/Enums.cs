using Godot;

namespace CyberRogueWars
{
	// ── Core Game Enums ─────────────────────────────────────────────────────

	/// <summary>Faction alignment for units, cards, and players.</summary>
	public enum FactionType
	{
		None,
		Voidlings,          // Lovecraftian horrors
		Confederation,      // Militaristic brotherhood
		ConglomerateOfOrion // Corporate AI hyperproducers
	}

	/// <summary>Unit classification for roguelite upgrades and summoning.</summary>
	public enum UnitType
	{
		Sludger,    // Default/example
		// TODO: Add more as you create UnitData assets
		// e.g. Drone, Guardian, SporeBeast, etc.
	}

	/// <summary>Unit role — useful for AI behaviors, targeting priorities, etc.</summary>
	public enum UnitRole
	{
		Infantry,
		Ranged,
		Support,
		Commander,
		Tank,
		Assassin
	}

	/// <summary>Card categories — drives UI icons and resolution logic.</summary>
	public enum CardType
	{
		Attack,
		Defense,
		Movement,
		Summon,
		Utility
	}

	/// <summary>Terrain types for the GameBoard (affects movement/defense).</summary>
	public enum TerrainType
	{
		Open,
		Cover,      // +1 Defense
		Hazard,     // -1 Defense
		Impassable  // Cannot move through
	}

	/// <summary>Turn phases in the 4-phase loop.</summary>
	public enum TurnPhase
	{
		Planning,
		Resolution,
		Unit,
		EndOfTurn
	}

	// Add more enums here as needed (e.g. for effects, alignments, etc.)
}
