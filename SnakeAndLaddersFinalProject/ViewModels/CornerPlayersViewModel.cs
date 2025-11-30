using System;
using System.Collections.Generic;
using SnakeAndLaddersFinalProject.GameplayService;
using SnakeAndLaddersFinalProject.ViewModels.Models;

namespace SnakeAndLaddersFinalProject.ViewModels
{
    public sealed class CornerPlayersViewModel
    {
        private const int MAX_CORNER_PLAYERS = 4;
        private const int SHIELD_TURNS = 3;
        private const int FREEZE_TURNS = 2;

        public LobbyMemberViewModel TopLeftPlayer { get; private set; }

        public LobbyMemberViewModel TopRightPlayer { get; private set; }

        public LobbyMemberViewModel BottomLeftPlayer { get; private set; }

        public LobbyMemberViewModel BottomRightPlayer { get; private set; }

        public CornerPlayersViewModel()
        {
            Reset();
        }

        public void Reset()
        {
            TopLeftPlayer = null;
            TopRightPlayer = null;
            BottomLeftPlayer = null;
            BottomRightPlayer = null;
        }

        public void InitializeFromLobbyMembers(IList<LobbyMemberViewModel> lobbyMembers)
        {
            Reset();

            if (lobbyMembers == null || lobbyMembers.Count == 0)
            {
                return;
            }

            int count = Math.Min(lobbyMembers.Count, MAX_CORNER_PLAYERS);

            if (count > 0)
            {
                TopLeftPlayer = lobbyMembers[0];
                ResetEffectsForSlot(TopLeftPlayer);
            }

            if (count > 1)
            {
                TopRightPlayer = lobbyMembers[1];
                ResetEffectsForSlot(TopRightPlayer);
            }

            if (count > 2)
            {
                BottomLeftPlayer = lobbyMembers[2];
                ResetEffectsForSlot(BottomLeftPlayer);
            }

            if (count > 3)
            {
                BottomRightPlayer = lobbyMembers[3];
                ResetEffectsForSlot(BottomRightPlayer);
            }
        }

        public void UpdateCurrentTurn(int currentTurnUserId)
        {
            UpdateIsCurrentTurn(TopLeftPlayer, currentTurnUserId);
            UpdateIsCurrentTurn(TopRightPlayer, currentTurnUserId);
            UpdateIsCurrentTurn(BottomLeftPlayer, currentTurnUserId);
            UpdateIsCurrentTurn(BottomRightPlayer, currentTurnUserId);
        }

        /// <summary>
        /// Llamar cuando termina el turno de un jugador para disminuir contadores
        /// de escudo / congelado y refrescar el texto.
        /// </summary>
        public void OnTurnAdvanced(int previousTurnUserId)
        {
            if (!TryFindSlotByUserId(previousTurnUserId, out LobbyMemberViewModel slot))
            {
                return;
            }

            if (slot.RemainingShieldTurns > 0)
            {
                slot.RemainingShieldTurns--;

                if (slot.RemainingShieldTurns <= 0)
                {
                    slot.RemainingShieldTurns = 0;
                    slot.HasShield = false;
                }
            }

            if (slot.RemainingFrozenTurns > 0)
            {
                slot.RemainingFrozenTurns--;

                if (slot.RemainingFrozenTurns <= 0)
                {
                    slot.RemainingFrozenTurns = 0;
                    slot.IsFrozen = false;
                }
            }

            RefreshEffectsTextForSlot(slot);
        }

        /// <summary>
        /// Aplica el resultado de un ítem a los jugadores en las esquinas
        /// para actualizar banderas y texto de efectos.
        /// </summary>
        public void ApplyItemEffect(ItemEffectResultDto effect)
        {
            if (effect == null)
            {
                return;
            }

            // Actor
            if (!TryFindSlotByUserId(effect.UserId, out LobbyMemberViewModel actorSlot))
            {
                return;
            }

            switch (effect.EffectType)
            {
                case ItemEffectType.Shield:
                    if (effect.ShieldActivated)
                    {
                        actorSlot.HasShield = true;
                        actorSlot.RemainingShieldTurns = SHIELD_TURNS;
                    }
                    else
                    {
                        actorSlot.HasShield = false;
                        actorSlot.RemainingShieldTurns = 0;
                    }

                    RefreshEffectsTextForSlot(actorSlot);
                    break;

                case ItemEffectType.Freeze:
                    if (!effect.TargetUserId.HasValue)
                    {
                        break;
                    }

                    if (!TryFindSlotByUserId(effect.TargetUserId.Value, out LobbyMemberViewModel targetSlot))
                    {
                        break;
                    }

                    if (effect.TargetFrozen)
                    {
                        targetSlot.IsFrozen = true;
                        targetSlot.RemainingFrozenTurns = FREEZE_TURNS;
                    }

                    RefreshEffectsTextForSlot(targetSlot);
                    break;

                // Anchor / Swap solo mueven fichas, no cambian texto de efectos
                case ItemEffectType.Anchor:
                case ItemEffectType.Swap:
                case ItemEffectType.Rocket:
                default:
                    break;
            }
        }

        public void UpdateEffectsText(int userId, string effectsText)
        {
            if (!TryFindSlotByUserId(userId, out LobbyMemberViewModel slot))
            {
                return;
            }

            slot.EffectsText = effectsText ?? string.Empty;
        }

        private static void UpdateIsCurrentTurn(
            LobbyMemberViewModel player,
            int currentTurnUserId)
        {
            if (player == null)
            {
                return;
            }

            player.IsCurrentTurn = player.UserId == currentTurnUserId;
        }

        private static void ResetEffectsForSlot(LobbyMemberViewModel slot)
        {
            if (slot == null)
            {
                return;
            }

            slot.HasShield = false;
            slot.RemainingShieldTurns = 0;
            slot.IsFrozen = false;
            slot.RemainingFrozenTurns = 0;
            slot.EffectsText = string.Empty;
        }

        private static void RefreshEffectsTextForSlot(LobbyMemberViewModel slot)
        {
            if (slot == null)
            {
                return;
            }

            var parts = new List<string>();

            if (slot.HasShield && slot.RemainingShieldTurns > 0)
            {
                parts.Add(string.Format("Escudo ({0} turnos)", slot.RemainingShieldTurns));
            }

            if (slot.IsFrozen && slot.RemainingFrozenTurns > 0)
            {
                parts.Add(string.Format("Congelado ({0} turnos)", slot.RemainingFrozenTurns));
            }

            slot.EffectsText = parts.Count == 0
                ? string.Empty
                : string.Join(" | ", parts);
        }

        private bool TryFindSlotByUserId(int userId, out LobbyMemberViewModel slot)
        {
            slot = null;

            if (TopLeftPlayer != null && TopLeftPlayer.UserId == userId)
            {
                slot = TopLeftPlayer;
            }
            else if (TopRightPlayer != null && TopRightPlayer.UserId == userId)
            {
                slot = TopRightPlayer;
            }
            else if (BottomLeftPlayer != null && BottomLeftPlayer.UserId == userId)
            {
                slot = BottomLeftPlayer;
            }
            else if (BottomRightPlayer != null && BottomRightPlayer.UserId == userId)
            {
                slot = BottomRightPlayer;
            }

            bool found = slot != null;
            return found;
        }
    }
}
