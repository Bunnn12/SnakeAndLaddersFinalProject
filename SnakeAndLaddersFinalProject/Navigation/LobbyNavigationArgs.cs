
namespace SnakeAndLaddersFinalProject.Navigation
{
    public enum LobbyEntryMode
    {
        Create,
        Join
    }

    public sealed class LobbyNavigationArgs
    {
        public LobbyEntryMode Mode { get; set; }
        public string JoinCode { get; set; } = string.Empty;
        public CreateMatchOptions CreateOptions { get; set; } 
    }
}
