using System.IO;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Enums;
using SpotifyAPI.Web.Models;
using System.Threading.Tasks;
using System;

namespace Spautofy
{
    class SpotifyAuth
    {
        public static SpotifyWebAPI _spotify = null;
        public static AuthorizationCodeAuth CodeAuth;
        //public static ImplicitGrantAuth ImplicitAuth;

        private static Token _Token;
        private static DateTime _Expires;

        private static string refreshTokenFile = "SpotifyAPI_RefreshToken.txt";


        /*public static async void SpotifyGetAuth()
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
        }*/

        public static async void SpotifyGetTokenAuth()
        {
            string _clientId = File.ReadAllText("SpotifyAPI_Key.txt");
            string _secret = File.ReadAllText("SpotifyAPI_Secret.txt");

            //https://johnnycrazy.github.io/SpotifyAPI-NET/docs/5.1.1/auth/authorization_code
            CodeAuth = new AuthorizationCodeAuth(
                _clientId,
                _secret,
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

            CodeAuth.AuthReceived += async (sender, payload) =>
            {
                CodeAuth.Stop();
                _Token = await CodeAuth.ExchangeCode(payload.Code);
                _Expires = DateTime.Now.AddSeconds(_Token.ExpiresIn);
                File.WriteAllText(refreshTokenFile, _Token.RefreshToken);

                _spotify = new SpotifyWebAPI()
                {
                    TokenType = _Token.TokenType,
                    AccessToken = _Token.AccessToken
                };
                //System.Windows.MessageBox.Show($"AuthReceived, RefreshToken:{_Token.RefreshToken}");
                //RefreshCycle(token);
            };

            //Load the refresh token from file if it exists - otherwise we have to launch a web page to get the token
            /*if (File.Exists(refreshTokenFile))
            {
                string refreshToken = File.ReadAllText(refreshTokenFile);

                if (!string.IsNullOrEmpty(refreshToken))
                {
                    _Token = await CodeAuth.RefreshToken(refreshToken);
                    System.Windows.MessageBox.Show($"File, RefreshToken:{_Token.RefreshToken}\bError:{_Token.Error}\n{_Token.ErrorDescription}\n{_Token.AccessToken}\n{_Token.TokenType}\n{_Token.ExpiresIn}");
                    File.WriteAllText(refreshTokenFile, _Token.RefreshToken);

                    _spotify = new SpotifyWebAPI()
                    {
                        TokenType = _Token.TokenType,
                        AccessToken = _Token.AccessToken
                    };

                    //RefreshCycle(token);
                }
                else //Blank file is getting saved sometimes, using this until I figure out why a refresh token isn't retrieved
                {
                    CodeAuth.Start();
                    CodeAuth.OpenBrowser();
                }
            }
            else
            {*/
            CodeAuth.Start();
            CodeAuth.OpenBrowser();
            //}
        }

        /// <summary>
        /// Check if the token needs to be refreshed to be able to play a song
        /// </summary>
        public static async Task CheckRefreshToken(int songMS)
        {
            //System.Windows.MessageBox.Show($"{DateTime.Now.AddMilliseconds(songMS).AddMinutes(5).ToLongTimeString()}\n{_Expires.ToLongTimeString()}");
            //If the token lasts longer than the song length + 5 minutes there is no need to refresh it
            if (DateTime.Now.AddMilliseconds(songMS).AddMinutes(5) < _Expires)
                return;

            _Token = await CodeAuth.RefreshToken(_Token.RefreshToken);
            _Expires = DateTime.Now.AddSeconds(_Token.ExpiresIn);
            File.WriteAllText(refreshTokenFile, _Token.RefreshToken);
            _spotify.TokenType = _Token.TokenType;
            _spotify.AccessToken = _Token.AccessToken;
        }

        /*private static async void RefreshCycle(Token token)
        {
            _RefreshToken = token.RefreshToken;
            File.WriteAllText(refreshTokenFile, _RefreshToken);

            double waitSeconds = token.ExpiresIn - 60;
            if (waitSeconds <= 0) waitSeconds = 1;

            //Keep running the entire time the app is running, otherwise music will stop or wont be playable after leaving it running
            //May also want/need to set it up so that it checks the token before each song starts, and refreshes if it will expire before the song finishes (or already expired)
            while (true)
            {
                await Task.Delay(System.TimeSpan.FromSeconds(waitSeconds));

                token = await CodeAuth.RefreshToken(_RefreshToken);
                _spotify.AccessToken = token.AccessToken;
                _spotify.TokenType = token.TokenType;
                _RefreshToken = token.RefreshToken;

                File.WriteAllText(refreshTokenFile, _RefreshToken);
            }
        }*/

    }
}

