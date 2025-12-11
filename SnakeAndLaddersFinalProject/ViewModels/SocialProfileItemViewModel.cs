using SnakeAndLaddersFinalProject.SocialProfileService;

namespace SnakeAndLaddersFinalProject.ViewModels
{
    public sealed class SocialProfileItemViewModel
    {
        public SocialNetworkType Network { get; }
        public string ProfileLink { get; private set; }

        public bool IsLinked
        {
            get { return !string.IsNullOrWhiteSpace(ProfileLink); }
        }

        public SocialProfileItemViewModel(SocialNetworkType network)
        {
            Network = network;
        }

        public void SetProfileLink(string link)
        {
            ProfileLink = link;
        }
    }
}
