namespace SnakeAndLaddersFinalProject.Utilities
{
    public static class AvatarPathUtility
    {
        private const string AVATAR_FOLDER = "Assets/Images/Avatars/";
        private const string AVATAR_EXTENSION = ".jpg";

        private const string PACK_URI_PREFIX = "pack://application:,,,/";
        private const string WPF_ASSEMBLY_NAME = "SnakeAndLaddersFinalProject";
        private const string PACK_COMPONENT_SEPARATOR = ";component/";

        public static string GetPackUri(string avatarId)
        {
            string normalizedAvatarId = AvatarIdHelper.NormalizeOrDefault(avatarId);

            return string.Concat(
                PACK_URI_PREFIX,
                WPF_ASSEMBLY_NAME,
                PACK_COMPONENT_SEPARATOR,
                AVATAR_FOLDER,
                normalizedAvatarId,
                AVATAR_EXTENSION);
        }
    }
}
