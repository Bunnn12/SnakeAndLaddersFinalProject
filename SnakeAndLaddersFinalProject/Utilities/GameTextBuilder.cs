using System;
using System.Collections.Generic;
using SnakeAndLaddersFinalProject.GameplayService;
using SnakeAndLaddersFinalProject.Properties.Langs;

namespace SnakeAndLaddersFinalProject.Game.Gameplay
{
    public static class GameTextBuilder
    {
        public static string BuildEffectsText(TokenStateDto tokenState)
        {
            List<string> effectTexts = new List<string>();

            if (tokenState.HasShield && tokenState.RemainingShieldTurns > 0)
            {
                effectTexts.Add(string.Format(Lang.GameEffectsShieldFmt, tokenState.RemainingShieldTurns));
            }

            if (tokenState.RemainingFrozenTurns > 0)
            {
                effectTexts.Add(string.Format(Lang.GameEffectsFrozenFmt, tokenState.RemainingFrozenTurns));
            }

            if (tokenState.HasPendingRocketBonus)
            {
                effectTexts.Add(Lang.GameEffectsRocketReadyText);
            }

            if (effectTexts.Count == 0)
            {
                return string.Empty;
            }

            return string.Join(Lang.GameEffectsSeparatorText, effectTexts);
        }

        public static string BuildItemUsedMessage(ItemUsedNotificationDto notification)
        {
            string actorPlayer = string.Format(Lang.PodiumDefaultPlayerNameFmt, notification.UserId);
            string targetPlayer = notification.TargetUserId.HasValue
                ? string.Format(Lang.PodiumDefaultPlayerNameFmt, notification.TargetUserId.Value)
                : null;

            ItemEffectResultDto effectResult = notification.EffectResult;
            bool isBlockedByShield = effectResult != null && effectResult.WasBlockedByShield;
            bool isWithNoMovement = effectResult != null && effectResult.FromCellIndex == effectResult.ToCellIndex;

            switch (notification.ItemCode)
            {
                case "IT_ROCKET":
                    if (isBlockedByShield && targetPlayer != null)
                    {
                        return string.Format(
                            Lang.GameItemRocketBlockedFmt,actorPlayer,targetPlayer);
                    }

                    return string.Format(Lang.GameItemRocketUsedFmt, actorPlayer);

                case "IT_ANCHOR":
                    if (isWithNoMovement && targetPlayer != null)
                    {
                        return string.Format(
                            Lang.GameItemAnchorBlockedFmt, actorPlayer,targetPlayer);
                    }

                    if (targetPlayer == null)
                    {
                        return string.Format( Lang.GameItemAnchorUsedFmt, actorPlayer);
                    }

                    return string.Format(Lang.GameItemAnchorUsedOnPlayerFmt, actorPlayer, targetPlayer);

                case "IT_FREEZE":
                    if (isBlockedByShield && targetPlayer != null)
                    {
                        return string.Format(Lang.GameItemFreezeBlockedFmt, actorPlayer, targetPlayer);
                    }

                    if (targetPlayer == null)
                    {
                        return string.Format(Lang.GameItemFreezeUsedFmt, actorPlayer);
                    }

                    return string.Format(Lang.GameItemFreezeAppliedFmt, actorPlayer, targetPlayer);

                case "IT_SHIELD":
                    return string.Format(Lang.GameItemShieldUsedFmt, actorPlayer);

                default:
                    return string.Format(Lang.GameItemGenericUsedFmt, actorPlayer);
            }
        }
    }
}
