using Godot;
using System.Collections.Generic;
using CyberRogueWars;
using CyberRogueWars.Core;

/// <summary>
/// Smoke-test script — attached to node_2d.tscn.
/// Exercises the Core data layer so you can confirm everything compiled
/// and the boost system is wired up correctly.
/// Delete or gut this once you start building real scenes! 🗑️
/// </summary>
public partial class TestScript : Node
{
    public override void _Ready()
    {
        GD.Print("=== Cyber Rogue Wars — Core Systems Smoke Test ===");
        GD.Print("");

        // ── 1. Build a unit blueprint ─────────────────────────────
        var sludgerData = new UnitData
        {
            UnitName     = "Sludger",
            UnitType     = UnitType.Sludger,
            Faction      = FactionType.Voidlings,
            Role         = UnitRole.Infantry,
            MaxHealth    = 20,
            AttackPower  = 4,
            Defense      = 1,
            MovementRange = 3,
            AttackRange  = 1,
            SupplyCost   = 2,
            Description  = "A shambling horror that never stops advancing."
        };

        GD.Print($"[Blueprint]  {sludgerData.UnitName} | ATK {sludgerData.AttackPower} | DEF {sludgerData.Defense} | MOV {sludgerData.MovementRange}");

        // ── 2. Create a player and spawn the unit ─────────────────
        var player = new PlayerData
        {
            PlayerName = "Player 1",
            Faction    = FactionType.Voidlings,
            IsHuman    = true
        };

        var sludger = new UnitInstance(sludgerData, player, startX: 0, startY: 3);
        player.AddUnit(sludger);
        player.UnlockedUnits.Add(UnitType.Sludger);

        GD.Print($"[Spawned]    {sludger}");
        GD.Print($"[Effective]  ATK {sludger.EffectiveAttack} | DEF {sludger.EffectiveDefense} | MOV {sludger.EffectiveMovement}  (no boosts yet — should match blueprint)");
        GD.Print("");

        // ── 3. Apply a roguelite upgrade boost ────────────────────
        GD.Print("[Draft]      Applying upgrade: Sludger ATK+2, MOV+1");
        var boosts = player.GetBoosts(UnitType.Sludger);
        boosts.AttackBonus   += 2;
        boosts.MovementBonus += 1;

        GD.Print($"[Boosts]     {boosts}");
        GD.Print($"[Effective]  ATK {sludger.EffectiveAttack} | DEF {sludger.EffectiveDefense} | MOV {sludger.EffectiveMovement}  (should be ATK {sludgerData.AttackPower + 2}, MOV {sludgerData.MovementRange + 1})");
        GD.Print("");

        // ── 4. Quick combat check ─────────────────────────────────
        GD.Print("[Combat]     Sludger takes 5 raw damage (DEF 1 → 4 actual)");
        int hpBefore = sludger.CurrentHealth;
        sludger.ApplyDamage(5);
        GD.Print($"[HP]         {hpBefore} → {sludger.CurrentHealth}  (expected {hpBefore - System.Math.Max(1, 5 - sludger.EffectiveDefense)})");
        GD.Print("");

        // ── 5. TurnManager smoke test ─────────────────────────────
        var player2 = new PlayerData { PlayerName = "Player 2", Faction = FactionType.Confederation, IsHuman = false, TurnOrderIndex = 1 };
        var tm = new TurnManager(new List<PlayerData> { player, player2 });

        GD.Print($"[TurnManager] Phase: {tm.CurrentPhase} | Active: {tm.ActivePlayer.PlayerName}");
        tm.AdvancePhase(); // Planning → Resolution
        GD.Print($"[TurnManager] Phase: {tm.CurrentPhase}");
        tm.AdvancePhase(); // Resolution → UnitPhase (resolves cards)
        GD.Print($"[TurnManager] Phase: {tm.CurrentPhase}");
        tm.AdvancePhase(); // UnitPhase → EndOfTurn
        tm.AdvancePhase(); // EndOfTurn → Planning (round 2)
        GD.Print($"[TurnManager] Round 2 started. Phase: {tm.CurrentPhase}");
        GD.Print("");

        GD.Print("=== All systems nominal! Go build something cool. 🚀 ===");
    }
}