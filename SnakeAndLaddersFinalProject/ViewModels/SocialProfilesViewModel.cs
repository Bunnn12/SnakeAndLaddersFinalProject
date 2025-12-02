using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Windows;
using log4net;
using SnakeAndLaddersFinalProject.SocialProfileService;
using Lang = SnakeAndLaddersFinalProject.Properties.Langs.Lang;

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

    public sealed class SocialProfilesViewModel
    {
        private const string SERVICE_ENDPOINT_CONFIGURATION_NAME = "NetTcpBinding_ISocialProfileService";

        private const string INSTAGRAM_HOST = "instagram.com";
        private const string FACEBOOK_HOST = "facebook.com";
        private const string TWITTER_HOST = "twitter.com";
        private const string X_HOST = "_x.com";

        private static readonly ILog Logger = LogManager.GetLogger(typeof(SocialProfilesViewModel));

        public SocialProfileItemViewModel Instagram { get; }

        public SocialProfileItemViewModel Facebook { get; }

        public SocialProfileItemViewModel Twitter { get; }

        public SocialProfilesViewModel()
        {
            Instagram = new SocialProfileItemViewModel(SocialNetworkType.Instagram);
            Facebook = new SocialProfileItemViewModel(SocialNetworkType.Facebook);
            Twitter = new SocialProfileItemViewModel(SocialNetworkType.Twitter);
        }

        public void LoadSocialProfiles(int userId)
        {
            var client = new SocialProfileServiceClient(SERVICE_ENDPOINT_CONFIGURATION_NAME);

            try
            {
                SocialProfileDto[] profiles = client.GetSocialProfiles(userId) ?? Array.Empty<SocialProfileDto>();

                ApplyProfileList(Instagram, profiles);
                ApplyProfileList(Facebook, profiles);
                ApplyProfileList(Twitter, profiles);
            }
            catch (Exception ex)
            {
                Logger.Error("Error loading social profiles.", ex);

                ApplyProfileList(Instagram, Array.Empty<SocialProfileDto>());
                ApplyProfileList(Facebook, Array.Empty<SocialProfileDto>());
                ApplyProfileList(Twitter, Array.Empty<SocialProfileDto>());
            }
            finally
            {
                SafeClose(client);
            }
        }

        public bool TryLinkProfile(int userId, SocialNetworkType network, string profileLink)
        {
            if (userId <= 0)
            {
                return false;
            }

            string trimmedProfileLink = profileLink == null ? string.Empty : profileLink.Trim();

            if (string.IsNullOrWhiteSpace(trimmedProfileLink))
            {
                MessageBox.Show(
                    Lang.SocialProfileLinkEmptyWarn,
                    Lang.UiTitleWarning,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return false;
            }

            if (!IsValidProfileLink(network, trimmedProfileLink))
            {
                MessageBox.Show(
                    Lang.SocialProfileInvalidUrlWarn,
                    Lang.UiTitleWarning,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return false;
            }

            var client = new SocialProfileServiceClient(SERVICE_ENDPOINT_CONFIGURATION_NAME);

            try
            {
                var request = new LinkSocialProfileRequestDto
                {
                    UserId = userId,
                    Network = network,
                    ProfileLink = trimmedProfileLink
                };

                SocialProfileDto result = client.LinkSocialProfile(request);

                if (result == null)
                {
                    MessageBox.Show(
                        Lang.SocialProfileSaveError,
                        Lang.UiTitleError,
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return false;
                }

                UpdateItemFromDto(result);

                MessageBox.Show(
                    Lang.SocialProfileLinkedInfo,
                    Lang.UiTitleInfo,
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Error linking social profile.", ex);
                MessageBox.Show(
                    Lang.SocialProfileLinkError,
                    Lang.UiTitleError,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return false;
            }
            finally
            {
                SafeClose(client);
            }
        }

        public bool TryUnlinkProfile(int userId, SocialNetworkType network)
        {
            if (userId <= 0)
            {
                return false;
            }

            var client = new SocialProfileServiceClient(SERVICE_ENDPOINT_CONFIGURATION_NAME);

            try
            {
                var request = new UnlinkSocialProfileRequestDto
                {
                    UserId = userId,
                    Network = network
                };

                client.UnlinkSocialProfile(request);

                GetItem(network)?.SetProfileLink(null);

                MessageBox.Show(
                    Lang.SocialProfileUnlinkedInfo,
                    Lang.UiTitleInfo,
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Error unlinking social profile.", ex);
                MessageBox.Show(
                    Lang.SocialProfileUnlinkError,
                    Lang.UiTitleError,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return false;
            }
            finally
            {
                SafeClose(client);
            }
        }

        private void ApplyProfileList(
            SocialProfileItemViewModel item,
            IEnumerable<SocialProfileDto> profiles)
        {
            if (item == null || profiles == null)
            {
                return;
            }

            foreach (SocialProfileDto dto in profiles)
            {
                if (dto == null)
                {
                    continue;
                }

                if (dto.Network == item.Network)
                {
                    item.SetProfileLink(dto.ProfileLink);
                    return;
                }
            }

            item.SetProfileLink(null);
        }

        private void UpdateItemFromDto(SocialProfileDto dto)
        {
            if (dto == null)
            {
                return;
            }

            var item = GetItem(dto.Network);
            if (item == null)
            {
                return;
            }

            item.SetProfileLink(dto.ProfileLink);
        }

        private SocialProfileItemViewModel GetItem(SocialNetworkType network)
        {
            switch (network)
            {
                case SocialNetworkType.Instagram:
                    return Instagram;
                case SocialNetworkType.Facebook:
                    return Facebook;
                case SocialNetworkType.Twitter:
                    return Twitter;
                default:
                    return null;
            }
        }

        private static void SafeClose(SocialProfileServiceClient client)
        {
            if (client == null)
            {
                return;
            }

            try
            {
                if (client.State == CommunicationState.Faulted)
                {
                    client.Abort();
                }
                else
                {
                    client.Close();
                }
            }
            catch
            {
                client.Abort();
            }
        }

        private static bool IsValidProfileLink(SocialNetworkType network, string profileLink)
        {
            if (string.IsNullOrWhiteSpace(profileLink))
            {
                return false;
            }

            string trimmed = profileLink.Trim();

            if (!Uri.TryCreate(trimmed, UriKind.Absolute, out Uri uri))
            {
                return false;
            }

            if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string host = uri.Host ?? string.Empty;

            switch (network)
            {
                case SocialNetworkType.Instagram:
                    if (!host.EndsWith(INSTAGRAM_HOST, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }

                    break;

                case SocialNetworkType.Facebook:
                    if (!host.EndsWith(FACEBOOK_HOST, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }

                    break;

                case SocialNetworkType.Twitter:
                    if (!host.EndsWith(TWITTER_HOST, StringComparison.OrdinalIgnoreCase) &&
                        !host.EndsWith(X_HOST, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }

                    break;

                default:
                    return false;
            }

            string path = uri.AbsolutePath ?? string.Empty;
            string trimmedPath = path.Trim('/');

            bool hasNonEmptyPath = !string.IsNullOrEmpty(trimmedPath);
            bool hasQuery = !string.IsNullOrEmpty(uri.Query);

            if (!hasNonEmptyPath && !hasQuery)
            {
                return false;
            }

            return true;
        }
    }
}
