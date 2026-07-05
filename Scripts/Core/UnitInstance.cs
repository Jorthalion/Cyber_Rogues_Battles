using Godot;

namespace CyberRogueWars
{
	/// <summary>
	/// A live unit currently on the game board.
	/// Created at runtime from a UnitData blueprint.
	/// Think of UnitData as the cookie cutter, UnitInstance as the cookie. 🍪
	/// </summary>
	public class UnitInstance
	{
		// ─── Blueprint Reference ───────────────────────────────────
		/// <summary>The static data this unit was created from.</summary>
		public UnitData Data { get; private set; }

		// ─── Ownership ────────────────────────────────────────────
		/// <summary>Which player controls this unit.</summary>
		public PlayerData Owner { get; set; }

		// ─── Live Stats ───────────────────────────────────────────
		/// <summary>Current HP — starts at Data.MaxHealth, ticks down when hit.</summary>
		public int CurrentHealth { get; private set; }

		/// <summary>Temporary armor bonus from a Defense card this turn.</summary>
		public int BonusArmor { get; set; } = 0;

		/// <summary>Temporary movement bonus from a card this turn.</summary>
		public int BonusMovement { get; set; } = 0;

		// ─── Board Position ───────────────────────────────────────
		/// <summary>Column on the 12x8 grid (0–11).</summary>
		public int GridX { get; set; } = 0;

		/// <summary>Row on the 12x8 grid (0–7).</summary>
		public int GridY { get; set; } = 0;

		// ─── Turn State Flags ─────────────────────────────────────
		/// <summary>Has this unit already moved this Unit Phase?</summary>
		public bool HasMoved { get; set; } = false;

		/// <summary>Has this unit already attacked this Unit Phase?</summary>
		public bool HasAttacked { get; set; } = false;

		// ─── Derived Properties ───────────────────────────────────
		public bool IsDead => CurrentHealth <= 0;

		/// <summary>
		/// Effective attack this turn — base stat + any roguelite boost the owner has drafted.
		/// If no owner or no boost entry exists yet, falls back to base stat. Safe to call anytime!
		/// </summary>
		public int EffectiveAttack
		{
			get
			{
				int bonus = Owner?.GetBoosts(Data.UnitType)?.AttackBonus ?? 0;
				return Data.AttackPower + bonus;
			}
		}

		/// <summary>Effective defense this turn — base + roguelite boost + any card-granted bonus armor.</summary>
		public int EffectiveDefense
		{
			get
			{
				int bonus = Owner?.GetBoosts(Data.UnitType)?.DefenseBonus ?? 0;
				return Data.Defense + BonusArmor + bonus;
			}
		}

		/// <summary>Effective movement range this turn — base + roguelite boost + any card-granted bonus.</summary>
		public int EffectiveMovement
		{
			get
			{
				int bonus = Owner?.GetBoosts(Data.UnitType)?.MovementBonus ?? 0;
				return Data.MovementRange + BonusMovement + bonus;
			}
		}

		// ─── Constructor ──────────────────────────────────────────
		/// <summary>
		/// Spawn a new unit from a blueprint.
		/// Always use this constructor — never create a bare UnitInstance.
		/// </summary>
		public UnitInstance(UnitData data, PlayerData owner, int startX = 0, int startY = 0)
		{
			Data          = data;
			Owner         = owner;
			CurrentHealth = data.MaxHealth;
			GridX         = startX;
			GridY         = startY;
		}

		// ─── Combat Helpers ───────────────────────────────────────

		/// <summary>
		/// Apply incoming damage after subtracting effective defense.
		/// Damage is always at least 1 (no healing from high armor).
		/// </summary>
		public void ApplyDamage(int rawDamage)
		{
			int actual = System.Math.Max(1, rawDamage - EffectiveDefense);
			CurrentHealth = System.Math.Max(0, CurrentHealth - actual);
		}

		/// <summary>Restore HP, capped at the blueprint's MaxHealth.</summary>
		public void Heal(int amount)
		{
			CurrentHealth = System.Math.Min(CurrentHealth + amount, Data.MaxHealth);
		}

		// ─── Turn Reset ───────────────────────────────────────────

		/// <summary>
		/// Called at the start of each Unit Phase to clear per-turn flags and bonuses.
		/// </summary>
		public void ResetForNewTurn()
		{
			HasMoved      = false;
			HasAttacked   = false;
			BonusArmor    = 0;
			BonusMovement = 0;
		}

		// ─── Debug ────────────────────────────────────────────────
		public override string ToString()
			=> $"{Data.UnitName} [{CurrentHealth}/{Data.MaxHealth} HP] @ ({GridX},{GridY})";
	}
}
