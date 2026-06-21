using System;
using System.Collections.Generic;
using System.Linq;

namespace CyberRogueWars
{
    /// <summary>
    /// Manages the 4-phase turn loop for CyberRogueWars:
    /// Planning → Resolution → Unit → EndOfTurn → (repeat)
    ///
    /// This is a pure C# logic class — no Godot Node inheritance,
    /// no GD.Print, no signals. Just the conductor waving the baton. 🎼
    /// </summary>
    public class TurnManager
    {
        // ─────────────────────────────────────────────
        //  References
        // ─────────────────────────────────────────────

        /// <summary>The shared game board (12×8 grid).</summary>
        public GameBoard Board { get; private set; }

        /// <summary>
        /// All players in turn-order. Index 0 goes first during Unit Phase.
        /// Rotated at the end of each round so the last player becomes first.
        /// </summary>
        public List<PlayerData> Players { get; private set; }

        // ─────────────────────────────────────────────
        //  State
        // ─────────────────────────────────────────────

        /// <summary>Which phase the game is currently in.</summary>
        public TurnPhase CurrentPhase { get; private set; }

        /// <summary>Round counter — starts at 1, increments each EndOfTurn.</summary>
        public int CurrentRound { get; private set; } = 1;

        /// <summary>
        /// Index into <see cref="Players"/> indicating whose Unit Phase turn it is.
        /// Advances via <see cref="EndUnitTurn"/>.
        /// </summary>
        public int CurrentPlayerIndex { get; private set; } = 0;

        // ─────────────────────────────────────────────
        //  Constructor
        // ─────────────────────────────────────────────

        /// <summary>
        /// Creates a new TurnManager.
        /// Call <see cref="BeginPlanningPhase"/> to kick off the first round.
        /// </summary>
        /// <param name="board">The shared GameBoard instance.</param>
        /// <param name="players">
        /// All players in starting turn order. Must contain at least 2 players.
        /// </param>
        public TurnManager(GameBoard board, List<PlayerData> players)
        {
            Board   = board   ?? throw new ArgumentNullException(nameof(board));
            Players = players ?? throw new ArgumentNullException(nameof(players));

            if (Players.Count < 2)
                throw new ArgumentException("CyberRogueWars needs at least 2 players!", nameof(players));

            // Stamp each player with their starting turn-order index.
            for (int i = 0; i < Players.Count; i++)
                Players[i].TurnOrderIndex = i;
        }

        // ─────────────────────────────────────────────
        //  Phase 1 — Planning
        // ─────────────────────────────────────────────

        /// <summary>
        /// Begins the Planning Phase.
        /// Clears every player's chosen card and resets all units'
        /// HasMoved / HasAttacked flags so the round starts fresh.
        /// </summary>
        public void BeginPlanningPhase()
        {
            CurrentPhase = TurnPhase.Planning;

            foreach (PlayerData player in Players)
            {
                // Wipe last round's card choice — everyone picks anew.
                player.ChosenCard = null;

                // Reset unit action flags for the coming Unit Phase.
                foreach (UnitInstance unit in player.ActiveUnits)
                    unit.ResetForNewTurn();
            }
        }

        // ─────────────────────────────────────────────
        //  Phase 1 → 2 — Card Submission
        // ─────────────────────────────────────────────

        /// <summary>
        /// Called when a player locks in their card choice for this round.
        /// Once ALL players have submitted, automatically advances to
        /// <see cref="BeginResolutionPhase"/>.
        /// </summary>
        /// <param name="player">The player submitting their card.</param>
        /// <param name="card">The card they chose to play this round.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if called outside the Planning Phase.
        /// </exception>
        public void SubmitCard(PlayerData player, CardData card)
        {
            if (CurrentPhase != TurnPhase.Planning)
                throw new InvalidOperationException(
                    $"SubmitCard called during {CurrentPhase} — cards can only be submitted during Planning!");

            if (player == null) throw new ArgumentNullException(nameof(player));
            if (card   == null) throw new ArgumentNullException(nameof(card));

            player.ChosenCard = card;

            // Check if every player has now submitted a card.
            bool allSubmitted = Players.All(p => p.ChosenCard != null);
            if (allSubmitted)
                BeginResolutionPhase();
        }

        // ─────────────────────────────────────────────
        //  Phase 2 — Resolution
        // ─────────────────────────────────────────────

        /// <summary>
        /// Begins the Resolution Phase.
        /// Players are sorted by their chosen card's Initiative (highest first),
        /// with ties broken by TurnOrderIndex (lower index wins the tie).
        /// Each card's effects are then applied in that order.
        /// Automatically advances to <see cref="BeginUnitPhase"/> when done.
        /// </summary>
        public void BeginResolutionPhase()
        {
            CurrentPhase = TurnPhase.Resolution;

            // Sort players: highest Initiative first; ties go to lower TurnOrderIndex.
            List<PlayerData> resolveOrder = Players
                .OrderByDescending(p => p.ChosenCard?.Initiative ?? 0)
                .ThenBy(p => p.TurnOrderIndex)
                .ToList();

            // Apply each card's effects in initiative order.
            foreach (PlayerData player in resolveOrder)
            {
                CardData card = player.ChosenCard;
                if (card == null) continue;   // Shouldn't happen, but safety first!

                ApplyCardEffects(player, card);
            }

            // Resolution is instant — straight into the Unit Phase.
            BeginUnitPhase();
        }

        /// <summary>
        /// Applies a single card's effects to its owning player.
        /// Damage and armor/movement bonuses are applied immediately.
        /// Summon effects are stubbed — wire up to GameBoard when ready.
        /// </summary>
        /// <param name="player">The player whose card is resolving.</param>
        /// <param name="card">The card being resolved.</param>
        private void ApplyCardEffects(PlayerData player, CardData card)
        {
            switch (card.Effect)
            {
                case CardEffect.Damage:
                    // Deal damage to all enemy commanders.
                    // "Enemy" = any player that isn't this one.
                    foreach (PlayerData enemy in Players)
                    {
                        if (enemy == player) continue;

                        UnitInstance commander = enemy.Commander;
                        if (commander != null && !commander.IsDead)
                            commander.ApplyDamage(card.EffectValue);
                    }
                    break;

                case CardEffect.ArmorBonus:
                    // Grant a temporary armor bonus to this player's commander.
                    // Stored as a runtime bonus — the commander's UnitData.Armor
                    // is the base; BonusArmor is the round-by-round modifier.
                    if (player.Commander != null && !player.Commander.IsDead)
                        player.Commander.BonusArmor += card.EffectValue;
                    break;

                case CardEffect.MovementBonus:
                    // Grant a temporary movement bonus to ALL of this player's units.
                    foreach (UnitInstance unit in player.ActiveUnits)
                    {
                        if (!unit.IsDead)
                            unit.BonusMovement += card.EffectValue;
                    }
                    break;

                case CardEffect.Summon:
                    // TODO: Instantiate a new UnitInstance from card.SummonUnitData,
                    // find a valid spawn cell near this player's deployment zone,
                    // call Board.PlaceUnit(), and add the unit to player.ActiveUnits.
                    // Leaving this as a stub until the spawn-zone system is designed.
                    break;

                default:
                    // Unknown effect — log it and move on rather than crashing.
                    // (Replace with proper logging once a logger is wired up.)
                    Console.WriteLine(
                        $"[TurnManager] Unhandled CardEffect '{card.Effect}' on card '{card.CardName}'. Skipping.");
                    break;
            }
        }

        // ─────────────────────────────────────────────
        //  Phase 3 — Unit Phase
        // ─────────────────────────────────────────────

        /// <summary>
        /// Begins the Unit Phase.
        /// Resets all units' action flags (HasMoved / HasAttacked) and
        /// sets <see cref="CurrentPlayerIndex"/> to 0 so the first player
        /// in the current turn order acts first.
        /// </summary>
        public void BeginUnitPhase()
        {
            CurrentPhase       = TurnPhase.Unit;
            CurrentPlayerIndex = 0;

            // Reset action flags — every unit gets a fresh set of actions.
            foreach (PlayerData player in Players)
                foreach (UnitInstance unit in player.ActiveUnits)
                    unit.ResetForNewTurn();
        }

        /// <summary>
        /// Called when the current player has finished moving and attacking
        /// with all their units (or chosen to pass).
        /// Advances to the next player; when all players are done,
        /// automatically advances to <see cref="BeginEndOfTurnPhase"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if called outside the Unit Phase.
        /// </exception>
        public void EndUnitTurn()
        {
            if (CurrentPhase != TurnPhase.Unit)
                throw new InvalidOperationException(
                    $"EndUnitTurn called during {CurrentPhase} — only valid during the Unit Phase!");

            CurrentPlayerIndex++;

            if (CurrentPlayerIndex >= Players.Count)
            {
                // All players have taken their unit turns — wrap up the round.
                BeginEndOfTurnPhase();
            }
            // Otherwise the next player's turn begins automatically.
            // (The UI / AI layer should watch CurrentPlayerIndex to know who acts next.)
        }

        /// <summary>
        /// Convenience property: the player whose Unit Phase turn it currently is.
        /// Returns null if the Unit Phase is not active.
        /// </summary>
        public PlayerData ActivePlayer =>
            CurrentPhase == TurnPhase.Unit && CurrentPlayerIndex < Players.Count
                ? Players[CurrentPlayerIndex]
                : null;

        // ─────────────────────────────────────────────
        //  Phase 4 — End of Turn
        // ─────────────────────────────────────────────

        /// <summary>
        /// Begins the End-of-Turn Phase.
        /// Rotates turn order (the first player moves to the back of the queue),
        /// ticks supply for all players, increments <see cref="CurrentRound"/>,
        /// then immediately begins the next round's Planning Phase.
        /// </summary>
        public void BeginEndOfTurnPhase()
        {
            CurrentPhase = TurnPhase.EndOfTurn;

            // ── Rotate turn order ──────────────────────────────────────────
            // The player who went first this round goes last next round.
            // This gives everyone a fair shot at the initiative advantage.
            if (Players.Count > 1)
            {
                PlayerData first = Players[0];
                Players.RemoveAt(0);
                Players.Add(first);

                // Re-stamp TurnOrderIndex to reflect the new order.
                for (int i = 0; i < Players.Count; i++)
                    Players[i].TurnOrderIndex = i;
            }

            // ── Tick supply for all players ────────────────────────────────
            foreach (PlayerData player in Players)
                player.TickSupply();

            // ── Clear per-round bonuses ────────────────────────────────────
            // BonusArmor and BonusMovement from cards only last one round.
            foreach (PlayerData player in Players)
            {
                foreach (UnitInstance unit in player.ActiveUnits)
                {
                    unit.BonusArmor     = 0;
                    unit.BonusMovement  = 0;
                }
            }

            // ── Advance round counter ──────────────────────────────────────
            CurrentRound++;

            // ── Begin the next round ───────────────────────────────────────
            BeginPlanningPhase();
        }

        // ─────────────────────────────────────────────
        //  Victory Check
        // ─────────────────────────────────────────────

        /// <summary>
        /// Checks whether any player has won the game.
        /// A player wins when ALL enemy commanders are dead.
        /// </summary>
        /// <returns>
        /// The winning <see cref="PlayerData"/>, or <c>null</c> if the game
        /// is still ongoing (or if multiple commanders died simultaneously —
        /// in that edge case, the first surviving player in the current turn
        /// order is returned as the winner).
        /// </returns>
        public PlayerData CheckVictory()
        {
            // Collect players whose commander is still alive.
            List<PlayerData> surviving = Players
                .Where(p => p.Commander != null && !p.Commander.IsDead)
                .ToList();

            // If exactly one player's commander survives, they win.
            if (surviving.Count == 1)
                return surviving[0];

            // If somehow ALL commanders are dead (mutual destruction),
            // return the first player in the current turn order as a tiebreak.
            // (Game designers: feel free to replace this with a proper draw state!)
            if (surviving.Count == 0 && Players.Count > 0)
                return Players[0];

            // Game is still going — no winner yet.
            return null;
        }

        // ─────────────────────────────────────────────
        //  Debug Helpers
        // ─────────────────────────────────────────────

        /// <summary>
        /// Returns a human-readable summary of the current turn state.
        /// Handy for console debugging — like a little status ticker! 📋
        /// </summary>
        public override string ToString()
        {
            string activeInfo = CurrentPhase == TurnPhase.Unit && ActivePlayer != null
                ? $" | Active: {ActivePlayer.PlayerName}"
                : string.Empty;

            return $"[Round {CurrentRound}] Phase: {CurrentPhase}{activeInfo} | " +
                   $"Turn order: {string.Join(" → ", Players.Select(p => p.PlayerName))}";
        }
    }
}