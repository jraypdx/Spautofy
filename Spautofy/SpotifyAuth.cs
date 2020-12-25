using System.IO;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Enums;

namespace Spautofy
{
    class SpotifyAuth
    {
        public static ImplicitGrantAuth ImplicitAuth;

        public static SpotifyWebAPI _spotify = null;

        public static async void SpotifyGetAuth()
        {
            string _clientId = File.ReadAllText("SpotifyAPI_Key.txt");
            ImplicitAuth = new ImplicitGrantAuth(
                _clientId,
                "http://localhost:6969/",
                "http://localhost:6969/",
                Scope.UserReadPrivate | //User info (name, email, etc.) probably not needed long term, using for testing
                Scope.UserReadRecentlyPlayed | //get list of recently played tracks - not used atm
                Scope.PlaylistReadPrivate | //read playlists
                Scope.UserReadCurrentlyPlaying | //current song info - also important
                Scope.UserModifyPlaybackState | //pause, play, seek, skip, add to queue - most important permission
                Scope.UserReadPlaybackState | //get info on current playing stuff - also important
                Scope.PlaylistModifyPrivate | //change and create playlists - only needed if we implement something for creating custom playlists through spotify (not likely?) - not used atm
                Scope.UserLibraryRead //Check/get saved albums/tracks - not used atm
                );
            ImplicitAuth.AuthReceived += async (sender, payload) =>
            {
                ImplicitAuth.Stop(); // `sender` is also the auth instance
                _spotify = new SpotifyWebAPI()
                {
                    TokenType = payload.TokenType,
                    AccessToken = payload.AccessToken
                };
                // Do requests with API client
            };
            ImplicitAuth.Start(); // Starts an internal HTTP Server
            ImplicitAuth.OpenBrowser();
        }
    }
}

