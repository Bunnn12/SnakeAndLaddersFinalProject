using System;
using System.Collections.Generic;
using SnakeAndLaddersFinalProject.GameplayService;

namespace SnakeAndLaddersFinalProject.Game.Gameplay
{
    public static class GameTextBuilder
    {
        public static string BuildEffectsText(TokenStateDto tokenState)
        {
            var parts = new List<string>();

            if (tokenState.HasShield && tokenState.RemainingShieldTurns > 0)
            {
                parts.Add(string.Format("Escudo ({0})", tokenState.RemainingShieldTurns));
            }

            if (tokenState.RemainingFrozenTurns > 0)
            {
                parts.Add(string.Format("Congelado ({0})", tokenState.RemainingFrozenTurns));
            }

            if (tokenState.HasPendingRocketBonus)
            {
                parts.Add("Propulsor listo");
            }

            if (parts.Count == 0)
            {
                return string.Empty;
            }

            return string.Join(" • ", parts);
        }

        public static string BuildItemUsedMessage(ItemUsedNotificationDto notification)
        {
            string actor = string.Format("Jugador {0}", notification.UserId);
            string target = notification.TargetUserId.HasValue
                ? string.Format("Jugador {0}", notification.TargetUserId.Value)
                : null;

            ItemEffectResultDto effect = notification.EffectResult;
            bool blockedByShield = effect != null && effect.WasBlockedByShield;
            bool noMovement = effect != null && effect.FromCellIndex == effect.ToCellIndex;

            switch (notification.ItemCode)
            {
                case "IT_ROCKET":
                    if (blockedByShield && target != null)
                    {
                        return string.Format(
                            "{0} intentó usar Cohete contra {1}, pero el escudo lo bloqueó.",
                            actor,
                            target);
                    }

                    return string.Format("{0} usó Cohete.", actor);

                case "IT_ANCHOR":
                    if (noMovement && target != null)
                    {
                        return string.Format(
                            "{0} intentó usar Ancla sobre {1}, pero ya está en la casilla inicial.",
                            actor,
                            target);
                    }

                    if (target == null)
                    {
                        return string.Format("{0} usó Ancla.", actor);
                    }

                    return string.Format("{0} usó Ancla contra {1}.", actor, target);

                case "IT_FREEZE":
                    if (blockedByShield && target != null)
                    {
                        return string.Format(
                            "{0} intentó congelar a {1}, pero el escudo lo bloqueó.",
                            actor,
                            target);
                    }

                    if (target == null)
                    {
                        return string.Format("{0} usó Congelar.", actor);
                    }

                    return string.Format("{0} congeló a {1}.", actor, target);

                case "IT_SHIELD":
                    return string.Format("{0} activó Escudo.", actor);

                default:
                    return string.Format("{0} usó un ítem.", actor);
            }
        }
    }
}
