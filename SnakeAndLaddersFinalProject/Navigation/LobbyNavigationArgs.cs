namespace SnakeAndLaddersFinalProject.Navigation
{
    public enum LobbyEntryMode
    {
        Create, // ser host (crear lobby)
        Join    // unirse por código
    }

    public sealed class LobbyNavigationArgs
    {
        public LobbyEntryMode Mode { get; set; }
        public string JoinCode { get; set; } // solo aplica cuando Mode = Join
    }
}
