using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.ServiceModel;
using System.Windows;
using log4net;
using SnakeAndLaddersFinalProject.SocialProfileService;
using SnakeAndLaddersFinalProject.Utilities;
using Lang = SnakeAndLaddersFinalProject.Properties.Langs.Lang;

namespace SnakeAndLaddersFinalProject.ViewModels
{
    public sealed class SocialProfilesViewModel
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(SocialProfilesViewModel));
        private const string SERVICE_ENDPOINT_CONFIGURATION_NAME = "NetTcpBinding_ISocialProfileService";

        private const string INSTAGRAM_HOST = "instagram.com";
        private const string FACEBOOK_HOST = "facebook.com";
        private const string TWITTER_HOST = "twitter.com";
        private const string X_HOST = "x.com"; 

        private const string INSTAGRAM_URL = "https://www.instagram.com/";
        private const string FACEBOOK_URL = "https://www.facebook.com/";
        private const string TWITTER_URL = "https://x.com/";

        private const int PROFILE_LINK_MIN_LENGTH = 10; 
        private const int PROFILE_LINK_MAX_LENGTH = 255;
        private const int MIN_VALID_USER_ID = 1;

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
                _logger.Error("Error loading social profiles.", ex);
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
            if (userId < MIN_VALID_USER_ID)
            {
                return false;
            }

            string normalizedProfileLink = InputValidator.Normalize(profileLink);

            if (!InputValidator.IsRequired(normalizedProfileLink))
            {
                ShowWarn(Lang.SocialProfileLinkEmptyWarn);
                return false;
            }

            if (!IsValidProfileLink(network, normalizedProfileLink))
            {
                ShowWarn(Lang.SocialProfileInvalidUrlWarn);
                return false;
            }

            var client = new SocialProfileServiceClient(SERVICE_ENDPOINT_CONFIGURATION_NAME);

            try
            {
                var request = new LinkSocialProfileRequestDto
                {
                    UserId = userId,
                    Network = network,
                    ProfileLink = normalizedProfileLink
                };

                SocialProfileDto result = client.LinkSocialProfile(request);

                if (result == null)
                {
                    ShowError(Lang.SocialProfileSaveError);
                    return false;
                }

                UpdateItemFromDto(result);
                ShowInfo(Lang.SocialProfileLinkedInfo);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error("Error linking social profile.", ex);
                ShowError(Lang.SocialProfileLinkError);
                return false;
            }
            finally
            {
                SafeClose(client);
            }
        }

        public bool TryUnlinkProfile(int userId, SocialNetworkType network)
        {
            if (userId < MIN_VALID_USER_ID)
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

                ShowInfo(Lang.SocialProfileUnlinkedInfo);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error("Error unlinking social profile.", ex);
                ShowError(Lang.SocialProfileUnlinkError);
                return false;
            }
            finally
            {
                SafeClose(client);
            }
        }

        private static bool IsValidProfileLink(SocialNetworkType network, string profileLink)
        {
            if (!InputValidator.IsUrl(profileLink, PROFILE_LINK_MIN_LENGTH, PROFILE_LINK_MAX_LENGTH))
            {
                return false;
            }

            if (!Uri.TryCreate(profileLink, UriKind.Absolute, out Uri uri))
            {
                return false;
            }

            string host = uri.Host ?? string.Empty;

            switch (network)
            {
                case SocialNetworkType.Instagram:
                    return host.EndsWith(INSTAGRAM_HOST, StringComparison.OrdinalIgnoreCase);

                case SocialNetworkType.Facebook:
                    return host.EndsWith(FACEBOOK_HOST, StringComparison.OrdinalIgnoreCase);

                case SocialNetworkType.Twitter:
                    return host.EndsWith(TWITTER_HOST, StringComparison.OrdinalIgnoreCase) ||
                           host.EndsWith(X_HOST, StringComparison.OrdinalIgnoreCase);

                default:
                    return false;
            }
        }

        public bool TryOpenNetworkHome(SocialNetworkType network)
        {
            string url;
            switch (network)
            {
                case SocialNetworkType.Instagram:
                    url = INSTAGRAM_URL;
                    break;
                case SocialNetworkType.Facebook:
                    url = FACEBOOK_URL;
                    break;
                case SocialNetworkType.Twitter:
                    url = TWITTER_URL;
                    break;
                default:
                    return false;
            }

            return TryOpenBrowser(url);
        }

        public bool TryOpenSavedProfile(SocialNetworkType network)
        {
            var item = GetItem(network);
            if (item == null || !item.IsLinked || string.IsNullOrWhiteSpace(item.ProfileLink))
            {
                ShowInfo(Lang.SocialNetworkNotLinkedInfo);
                return false;
            }

            string normalizedProfileLink = InputValidator.Normalize(item.ProfileLink);

            if (!IsValidProfileLink(network, normalizedProfileLink))
            {
                ShowWarn(Lang.SocialProfileInvalidUrlWarn);
                return false;
            }

            return TryOpenBrowser(normalizedProfileLink);
        }

        private bool TryOpenBrowser(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error("Error opening browser.", ex);
                ShowError(Lang.SocialBrowserOpenError);
                return false;
            }
        }

        private void ApplyProfileList(SocialProfileItemViewModel item, IEnumerable<SocialProfileDto> profiles)
        {
            if (item == null || profiles == null)
            {
                return;
            }

            foreach (SocialProfileDto dto in profiles)
            {
                if (dto != null && dto.Network == item.Network)
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
            GetItem(dto.Network)?.SetProfileLink(dto.ProfileLink);
        }

        private SocialProfileItemViewModel GetItem(SocialNetworkType network)
        {
            switch (network)
            {
                case SocialNetworkType.Instagram: return Instagram;
                case SocialNetworkType.Facebook: return Facebook;
                case SocialNetworkType.Twitter: return Twitter;
                default: return null;
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

        private static void ShowWarn(string message)
        {
            MessageBox.Show(message, Lang.UiTitleWarning, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private static void ShowInfo(string message)
        {
            MessageBox.Show(message, Lang.UiTitleInfo, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private static void ShowError(string message)
        {
            MessageBox.Show(message, Lang.UiTitleError, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
