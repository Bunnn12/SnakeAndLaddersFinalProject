using System;

namespace SnakeAndLaddersFinalProject.Utilities
{
    public static class AvatarPathUtility
    {
        private const string AvatarFolder = "Assets/Images/Avatars/";
        private const string AvatarExtension = ".jpg";

        /// <summary>
        /// Devuelve una pack URI lista para usar en Image.Source.
        /// </summary>
        public static string GetPackUri(string avatarId)
        {
            string normalizedId = AvatarIdHelper.NormalizeOrDefault(avatarId);

            // SnakeAndLaddersFinalProject = nombre del assembly WPF
            return string.Concat(
                "pack://application:,,,/SnakeAndLaddersFinalProject;",
                "component/",
                AvatarFolder,
                normalizedId,
                AvatarExtension);
        }
    }
}
