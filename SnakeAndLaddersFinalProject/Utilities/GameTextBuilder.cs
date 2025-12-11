using System;
using System.Collections.Generic;
using SnakeAndLaddersFinalProject.GameplayService;
using SnakeAndLaddersFinalProject.Properties.Langs;

namespace SnakeAndLaddersFinalProject.Game.Gameplay
{
    public static class GameTextBuilder
    {
        private const string ITEM_CODE_ROCKET = "IT_ROCKET";
        private const string ITEM_CODE_ANCHOR = "IT_ANCHOR";
        private const string ITEM_CODE_FREEZE = "IT_FREEZE";
        private const string ITEM_CODE_SHIELD = "IT_SHIELD";

        public static string BuildEffectsText(TokenStateDto tokenState)
        {
            List<string> effectTexts = new List<string>();

            if (tokenState.HasShield && tokenState.RemainingShieldTurns > 0)
            {
                effectTexts.Add(
                    string.Format(Lang.GameEffectsShieldFmt, tokenState.RemainingShieldTurns));
            }

            if (tokenState.RemainingFrozenTurns > 0)
            {
                effectTexts.Add(
                    string.Format(Lang.GameEffectsFrozenFmt, tokenState.RemainingFrozenTurns));
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
            if (notification == null)
            {
                throw new ArgumentNullException(nameof(notification));
            }

            string actorPlayer = FormatPlayerName(notification.UserId);
            string targetPlayer = FormatTargetPlayerName(notification.TargetUserId);

            ItemEffectResultDto effectResult = notification.EffectResult;
            bool isBlockedByShield = IsBlockedByShield(effectResult);
            bool isWithNoMovement = IsWithNoMovement(effectResult);

            switch (notification.ItemCode)
            {
                case ITEM_CODE_ROCKET:
                    return BuildRocketMessage(actorPlayer, targetPlayer, isBlockedByShield);

                case ITEM_CODE_ANCHOR:
                    return BuildAnchorMessage(actorPlayer, targetPlayer, isWithNoMovement);

                case ITEM_CODE_FREEZE:
                    return BuildFreezeMessage(actorPlayer, targetPlayer, isBlockedByShield);

                case ITEM_CODE_SHIELD:
                    return string.Format(Lang.GameItemShieldUsedFmt, actorPlayer);

                default:
                    return string.Format(Lang.GameItemGenericUsedFmt, actorPlayer);
            }
        }

        private static string FormatPlayerName(int userId)
        {
            return string.Format(Lang.PodiumDefaultPlayerNameFmt, userId);
        }

        private static string FormatTargetPlayerName(int? targetUserId)
        {
            if (!targetUserId.HasValue)
            {
                return null;
            }

            return FormatPlayerName(targetUserId.Value);
        }

        private static bool IsBlockedByShield(ItemEffectResultDto effectResult)
        {
            return effectResult != null && effectResult.WasBlockedByShield;
        }

        private static bool IsWithNoMovement(ItemEffectResultDto effectResult)
        {
            return effectResult != null &&
                   effectResult.FromCellIndex == effectResult.ToCellIndex;
        }

        private static string BuildRocketMessage(string actorPlayer,
            string targetPlayer, bool isBlockedByShield)
        {
            if (isBlockedByShield && targetPlayer != null)
            {
                return string.Format(
                    Lang.GameItemRocketBlockedFmt,
                    actorPlayer,
                    targetPlayer);
            }

            return string.Format(Lang.GameItemRocketUsedFmt, actorPlayer);
        }

        private static string BuildAnchorMessage(
            string actorPlayer,
            string targetPlayer,
            bool isWithNoMovement)
        {
            if (isWithNoMovement && targetPlayer != null)
            {
                return string.Format(
                    Lang.GameItemAnchorBlockedFmt,
                    actorPlayer,
                    targetPlayer);
            }

            if (targetPlayer == null)
            {
                return string.Format(Lang.GameItemAnchorUsedFmt, actorPlayer);
            }

            return string.Format(
                Lang.GameItemAnchorUsedOnPlayerFmt,
                actorPlayer,
                targetPlayer);
        }

        private static string BuildFreezeMessage(string actorPlayer, string targetPlayer,
            bool isBlockedByShield)
        {
            if (isBlockedByShield && targetPlayer != null)
            {
                return string.Format(
                    Lang.GameItemFreezeBlockedFmt,
                    actorPlayer,
                    targetPlayer);
            }

            if (targetPlayer == null)
            {
                return string.Format(Lang.GameItemFreezeUsedFmt, actorPlayer);
            }

            return string.Format(
                Lang.GameItemFreezeAppliedFmt,
                actorPlayer,
                targetPlayer);
        }
    }
}
