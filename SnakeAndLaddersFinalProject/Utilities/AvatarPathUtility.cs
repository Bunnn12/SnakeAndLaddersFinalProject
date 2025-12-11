namespace SnakeAndLaddersFinalProject.Utilities
{
    public static class AvatarPathUtility
    {
        private const string AVATAR_FOLDER = "Assets/Images/Avatars/";
        private const string AVATAR_EXTENSION = ".jpg";

        private const string WPF_ASSEMBLY_NAME = "SnakeAndLaddersFinalProject";
        private const string PACK_COMPONENT_SEPARATOR = ";component/";

        public static string GetPackUri(string avatarId)
        {
            string normalizedAvatarId = AvatarIdHelper.NormalizeOrDefault(avatarId);

            string relativePath = string.Concat(
                "/", WPF_ASSEMBLY_NAME, PACK_COMPONENT_SEPARATOR,
                AVATAR_FOLDER, normalizedAvatarId, AVATAR_EXTENSION);

            return relativePath;
        }
    }
}
