using Godot;
using CyberRogueWars.Core;   // ← ADD THIS LINE

namespace CyberRogueWars
{
	/// <summary>
	/// Blueprint resource for an action card used during the Planning Phase.
	/// One CardData asset lives in Resources/ per card type.
	/// At runtime a player holds a hand of these and picks one per turn.
	/// </summary>
	[GlobalClass]
	public partial class CardData : Resource
	{
		// ─── Identity ─────────────────────────────────────────────
		[Export] public string CardName     { get; set; } = "Unknown Card";
		[Export] public CardType Type       { get; set; } = CardType.Attack;
		[Export] public FactionType Faction { get; set; } = FactionType.Confederation;

		// ─── Gameplay Values ──────────────────────────────────────
		/// <summary>Initiative bonus — higher goes first in Resolution Phase.</summary>
		[Export] public int Initiative      { get; set; } = 0;

		/// <summary>Flat damage dealt when this card resolves (Attack cards).</summary>
		[Export] public int Damage          { get; set; } = 0;

		/// <summary>Flat armor granted when this card resolves (Defense cards).</summary>
		[Export] public int ArmorBonus      { get; set; } = 0;

		/// <summary>Extra movement tiles granted this turn (any card).</summary>
		[Export] public int MovementBonus   { get; set; } = 0;

		/// <summary>
		/// For Summon cards — which UnitData blueprint to spawn.
		/// Leave null for non-summon cards.
		/// </summary>
		[Export] public UnitData SummonUnit { get; set; } = null;

		// ─── Presentation ─────────────────────────────────────────
		[Export] public string Description  { get; set; } = "";
		[Export] public Texture2D CardArt   { get; set; } = null;

		// ─── Roguelite Rarity ─────────────────────────────────────
		/// <summary>
		/// Weight used by UpgradeManager when building the 4-card draft pool.
		/// Lower = rarer. Common = 10, Uncommon = 5, Rare = 2, Legendary = 1.
		/// </summary>
		[Export] public int DraftWeight     { get; set; } = 10;
	}
}
