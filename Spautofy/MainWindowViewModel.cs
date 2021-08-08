using DirectShowLib;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using SpotifyAPI.Web.Models;
using SpotifyAPI.Web;
using System.IO;
using System.Diagnostics;
using System.Net;

namespace Spautofy
{
    class MainWindowViewModel : ViewModelBase //TODO:  right click remove isn't working, set up way to rearrange queue items
    {
        private MainWindowModel _model; //Ended up implementing pretty much everything in the VM here...
        private bool IsRecording;

        public ICommand RecordButton_Command { get; }
        public ICommand GetUserInfo_Command { get; }
        public ICommand GetSongInfo_Command { get; }
        public ICommand ShowQueue_Command { get; }
        public ICommand RemoveItem_Command { get; }
        public ICommand InfoButton_Command { get; }

        public MainWindowViewModel()
        {
            //SpotifyAuth.SpotifyGetAuth(); //has to pop open a browser to authorize the app each time, only way to do it without backend/server code?
            SpotifyAuth.SpotifyGetTokenAuth();
            Process.Start(new ProcessStartInfo("cmd", $"/c start {"https://open.spotify.com/"}")); //open spotify

            DisplayImage.img = "spautofy.jpg";
            _model = new MainWindowModel();
            IsRecording = false;
            RecordButton_Command = new DelegateCommand(x => RecordButton_Function());
            GetUserInfo_Command = new DelegateCommand(x => GetUserInfo_Function());
            GetSongInfo_Command = new DelegateCommand(x => GetSongInfo_Function());
            ShowQueue_Command = new DelegateCommand(x => ShowQueue_Function());
            RemoveItem_Command = new DelegateCommand(x => ListBoxQueue.Remove((QueueItem)x));
            InfoButton_Command = new DelegateCommand(x => InfoButton_Function());
            Task.Run(async () => await UpdateVolumeBarTask());
        }



        public Visibility Queue_Visibility
        {
            get => _model.Queue_Visibility;
            set
            {
                _model.Queue_Visibility = value;
                OnPropertyChanged(nameof(Queue_Visibility));
            }
        }
        public ObservableCollection<QueueItem> ListBoxQueue
        {
            get => _model.ListBoxQueue;
            set
            {
                _model.ListBoxQueue = value;
                OnPropertyChanged(nameof(ListBoxQueue));
            }
        }
        /*public WaveOutCapabilities SelectedAudioDevice
        {
            get => _model.SelectedAudioDevice;
            set
            {
                _model.SelectedAudioDevice = value;
                OnPropertyChanged(nameof(SelectedAudioDevice));
            }
        }*/
        public string DragDropSpotifyLinkBox 
        {
            get => _model.DragDropSpotifyLinkBox;
            set
            {
                _model.DragDropSpotifyLinkBox = value;
                OnPropertyChanged(nameof(DragDropSpotifyLinkBox));
                Task.Run(() => UpdateSpotifyLinkBox());
            }
        }
        public string DisplayPlayingImage_Output {
            get => DisplayImage.img;
            set
            {
                DisplayImage.img = value;
                OnPropertyChanged(nameof(DisplayPlayingImage_Output));
            }
        }
        public float NowPlayingProgressBar_Output
        {
            get => _model.NowPlayingProgressBar_Output;
            set
            {
                _model.NowPlayingProgressBar_Output = value;
                OnPropertyChanged(nameof(NowPlayingProgressBar_Output));
            }
        }
        public float SystemVolumeProgressBar_Output
        {
            get => _model.SystemVolumeProgressBar_Output;
            set
            {
                _model.SystemVolumeProgressBar_Output = value;
                OnPropertyChanged(nameof(SystemVolumeProgressBar_Output));
            }
        }
        public string SpotifyTextoutput
        {
            get => _model.SpotifyTextoutput;
            set
            {
                _model.SpotifyTextoutput = value;
                OnPropertyChanged(nameof(SpotifyTextoutput));
            }
        }
        public string NowPlayingOutput
        {
            get => _model.NowPlayingOutput;
            set
            {
                _model.NowPlayingOutput = value;
                OnPropertyChanged(nameof(NowPlayingOutput));
            }
        }
        //public string RecordButtonText
        //{
        //    get => _model.RecordButtonText;
        //    set
        //    {
        //        _model.RecordButtonText = value;
        //        OnPropertyChanged(nameof(RecordButtonText));
        //    }
        //}

        public async void RecordButton_Function()
        {
            if (IsRecording == false)
            {
                IsRecording = true;
                await StartRecording();
                IsRecording = false;
            }
            else
                return;
        }

        public async Task StartRecording()
        {
            string linkWithID = DragDropSpotifyLinkBox;

            if (ListBoxQueue.Count < 1)
                return;
            else
            {
                foreach (var queue in ListBoxQueue) //why isn't it moving on to the next queue item?
                {
                    try
                    {
                        NowPlayingOutput = "Preparing";
                        //string ID = linkWithID.Split('/').Last();
                        //RecordButtonText = "RECORDING";
                        if (queue.Type == QueueItemType.Track)
                        {
                            var single = new Record(queue.GetID(), Record.SpotifyType.track);
                            await single.WaitForReady();

                            Task.Run(async () => { DisplayPlayingImage_Output = await single.GetCoverArt(); }); //update picture without freezing UI thread
                            NowPlayingOutput = $"{String.Join(", ", single.Artists)}\n{single.Title}";
                            UpdateProgressBarTask(single.DurationMS);
                            await Task.Run(() => single.StartRecording());
                            NowPlayingOutput = "Trimming silence";
                            await Task.Run(() => single.TrimWavSilence());
                            NowPlayingOutput = "Converting to MP3";
                            await Task.Run(() => single.ConvertToMP3());
                            NowPlayingOutput = "Adding ID3 tags";
                            await Task.Run(() => single.AddID3Tags());
                            NowPlayingOutput = "Done";
                        }
                        else if (queue.Type == QueueItemType.Album)
                        {
                            var album = new Record(queue.GetID(), Record.SpotifyType.album);
                            await album.WaitForReady();

                            while (await album.LoadNextSongAlbum())
                            {
                                NowPlayingOutput = $"{String.Join(", ", album.Artists)}\n{album.Title}";
                                Task.Run(async () => { DisplayPlayingImage_Output = await album.GetCoverArt(); }); //update picture without freezing UI thread
                                UpdateProgressBarTask(album.DurationMS);
                                await Task.Run(() => album.StartRecording());
                                NowPlayingOutput = "Trimming silence";
                                await Task.Run(() => album.TrimWavSilence());
                                NowPlayingOutput = "Converting to MP3";
                                await Task.Run(() => album.ConvertToMP3());
                                NowPlayingOutput = "Adding ID3 tags";
                                await Task.Run(() => album.AddID3Tags());
                                NowPlayingOutput = "Loading next song";
                            }
                            NowPlayingOutput = "Done";
                        }
                        else if (queue.Type == QueueItemType.Playlist)
                        {
                            var playlist = new Record(queue.GetID(), Record.SpotifyType.playlist);
                            await playlist.WaitForReady();

                            while (await playlist.LoadNextSongPlaylist())
                            {
                                NowPlayingOutput = $"{String.Join(", ", playlist.Artists)}\n{playlist.Title}";
                                Task.Run(async () => { DisplayPlayingImage_Output = await playlist.GetCoverArt(); }); //update picture without freezing UI thread
                                UpdateProgressBarTask(playlist.DurationMS);
                                await Task.Run(() => playlist.StartRecording());
                                NowPlayingOutput = "Trimming silence";
                                await Task.Run(() => playlist.TrimWavSilence());
                                NowPlayingOutput = "Converting to MP3";
                                await Task.Run(() => playlist.ConvertToMP3());
                                NowPlayingOutput = "Adding ID3 tags";
                                await Task.Run(() => playlist.AddID3Tags());
                                NowPlayingOutput = "Loading next song";
                            }
                            NowPlayingOutput = "Done";
                            //MessageBox.Show("Playlist placeholder, nothing here yet");
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"{ex.Message}\n{ex.StackTrace}");
                    }
                }
            }
            //RecordButtonText = "Start Recording";
            NowPlayingOutput = "";
        }


        public void ShowQueue_Function()
        {
            if (Queue_Visibility == Visibility.Collapsed)
                Queue_Visibility = Visibility.Visible;
            else
                Queue_Visibility = Visibility.Collapsed;
        }
        public void GetUserInfo_Function()
        {
            try
            {
                PrivateProfile temp = SpotifyAuth._spotify.GetPrivateProfile();
                string outString = $"Name: {temp.DisplayName}\n" +
                    $"Type: {temp.Type}\n" +
                    $"Uri: {temp.Uri}\n" +
                    $"ID: {temp.Id}\n" +
                    $"Country: {temp.Country}\n" +
                    $"Birthdate: {temp.Birthdate}\n" +
                    $"Email: {temp.Email}\n";
                SpotifyTextoutput = outString;
            }
            catch (Exception ex)
            {
                SpotifyTextoutput = "User needs to authenticate";
            }
        }

        public void InfoButton_Function()
        {
            Task.Run(() =>
            {
                MessageBox.Show("Click on the playlist button in the top left, then add music by dragging a song, album, or playist from spotify in to the queue.\n" +
                    "When you're ready to start, close the queue and click the play/pause button below it\n\n" +
                    "For best results recording, make sure no other sounds play (mute system sounds and other apps in Windows!) and that you use a good sound card at full volume with Spotify premium sound quality set to highest");
            });
        }
        public async void GetSongInfo_Function()
        {
            PlaybackContext temp = null;
            try
            {
                //var song = await SpotifyAuth._spotify.GetPlayingTrackAsync();
                temp = SpotifyAuth._spotify.GetPlayback();
                var song = temp.Item;
                string outString = $"{song.Name}\n" +
                    $"{song.Artists}\n" +
                    $"{temp.CurrentlyPlayingType}\n" +
                    $"{temp.ProgressMs}";
                    //$"Name: {song.Item.Name}\n" +
                    //$"Artists: {song.Item.Artists.ToString()}\n" +
                    //$"Album: {song.Item.Album.Name}\n" +
                    //$"#: {song.Item.TrackNumber}" +
                    //$"{song.ProgressMs}";
                SpotifyTextoutput = outString;
            }
            catch (Exception ex)
            {
                SpotifyTextoutput = $"{temp?.Error.Message}";
                //SpotifyAuth.SpotifyGetAuth();
                //GetSongInfo_Function();
            }
        }
        private async Task UpdateVolumeBarTask()
        {
            var device = new NAudio.CoreAudioApi.MMDeviceEnumerator();
            while (true)
            {
                var vol = device.GetDefaultAudioEndpoint(NAudio.CoreAudioApi.DataFlow.Render, NAudio.CoreAudioApi.Role.Console).AudioMeterInformation.MasterPeakValue;
                SystemVolumeProgressBar_Output = vol;
                await Task.Delay(2);
            }
        }

        private async Task UpdateProgressBarTask(int durationMS)
        {
            int elapsedMS = 0;
            await Task.Delay(2000); //takes about 2 seconds for the song to start playing
            while(IsRecording && elapsedMS < durationMS)
            {
                elapsedMS += 512;
                NowPlayingProgressBar_Output = (float) elapsedMS / durationMS;
                await Task.Delay(512);
                //MessageBox.Show($"{NowPlayingProgressBar_Output}\n{(float) elapsedMS / durationMS}\n{elapsedMS} {durationMS}");
            }
        }

        public async void UpdateSpotifyLinkBox()
        {
            NowPlayingOutput = "Getting info";
            string linkWithID = DragDropSpotifyLinkBox;
            string ID = linkWithID.Split('/').Last();
            string cacheDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cache");
            if (!Directory.Exists(cacheDir))
                Directory.CreateDirectory(cacheDir);

            try
            {
                if (linkWithID.Contains("track"))
                {
                    var track = await SpotifyAuth._spotify.GetTrackAsync(ID);
                    var artists = new List<string>();
                    foreach (var a in track.Artists)
                        artists.Add(a.Name);

                    Image LargestImage = null;

                    foreach (var i in track.Album.Images)
                        if (LargestImage == null || i.Width > LargestImage.Width) LargestImage = i;

                    if (LargestImage != null)
                    {
                        string outputImage = Path.Combine(cacheDir, ID + ".jpg");
                        using (WebClient client = new WebClient())
                            client.DownloadFile(LargestImage.Url, outputImage);
                        DisplayPlayingImage_Output = outputImage;
                    }

                    NowPlayingOutput = $"--- Track ---\n{String.Join(", ", artists)}\n{track.Name}\n{track.Album.Name}";
                }
                else if (linkWithID.Contains("album"))
                {
                    var album = await SpotifyAuth._spotify.GetAlbumAsync(ID);
                    var artists = new List<string>();
                    foreach (var a in album.Artists)
                        artists.Add(a.Name);

                    NowPlayingOutput = $"--- Album ---\n{String.Join(", ", artists)}\n{album.Name}";

                    Image LargestImage = null;

                    foreach (var i in album.Images)
                        if (LargestImage == null || i.Width > LargestImage.Width) LargestImage = i;

                    if (LargestImage != null)
                    {
                        string outputImage = Path.Combine(cacheDir, ID + ".jpg");
                        if (!File.Exists(outputImage))
                        {
                            using (WebClient client = new WebClient())
                                client.DownloadFile(LargestImage.Url, outputImage);
                        }
                        DisplayPlayingImage_Output = outputImage;
                    }
                }
                else if (linkWithID.Contains("playlist"))
                {
                    var playlist = await SpotifyAuth._spotify.GetPlaylistAsync(ID);

                    Image OwnerImage = null;

                    foreach (var i in playlist.Images)
                        if (OwnerImage == null || i.Width > OwnerImage.Width) OwnerImage = i;

                    NowPlayingOutput = $"--- Playlist ---\n{playlist.Owner.DisplayName}\n{playlist.Name}";

                    if (OwnerImage != null)
                    {
                        string outputImage = Path.Combine(cacheDir, ID + ".jpg");
                        if (!File.Exists(outputImage))
                        {
                            using (WebClient client = new WebClient())
                                client.DownloadFile(OwnerImage.Url, outputImage);
                        }
                        DisplayPlayingImage_Output = outputImage;
                    }
                }
                else
                {
                    NowPlayingOutput = "";
                }
            }
            catch (Exception ex)
            {
                NowPlayingOutput = $"ERROR:  Not a valid Spotify link\n{ex.Message}";
                MessageBox.Show($"{ex.Message}\n{ex.StackTrace}");
            }
        }




        /*public void ListBoxQueue_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.StringFormat))
                e.Effects = DragDropEffects.All;
            else
                e.Effects = DragDropEffects.None;
        }*/

        /// <summary>
        /// Used to add to queue when user drops a link in
        /// </summary>
        public async void ListBoxQueue_DragDrop(string userDrop)
        {
            QueueItem toAdd = new QueueItem(userDrop);
            var artists = new List<string>();
            Image LargestImage = null;

            string ID = userDrop.Split('/').Last();
            string cacheDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cache");
            string imgDir = Path.Combine(cacheDir, ID + ".jpg");
            if (!Directory.Exists(cacheDir))
                Directory.CreateDirectory(cacheDir);

            try
            {
                if (userDrop.Contains("track"))
                {
                    var track = await SpotifyAuth._spotify.GetTrackAsync(ID);

                    foreach (var a in track.Artists)
                        artists.Add(a.Name);

                    toAdd.Artist = String.Join(" & ", artists);
                    toAdd.Title = track.Name;
                    toAdd.Type = QueueItemType.Track;
                    toAdd.TypeString = "Track";

                    foreach (var i in track.Album.Images)
                        if (LargestImage == null || i.Width > LargestImage.Width) LargestImage = i;
                }
                else if (userDrop.Contains("album"))
                {
                    var album = await SpotifyAuth._spotify.GetAlbumAsync(ID);

                    foreach (var a in album.Artists)
                        artists.Add(a.Name);

                    toAdd.Artist = String.Join(" & ", artists);
                    toAdd.Title = album.Name;
                    toAdd.Type = QueueItemType.Album;
                    toAdd.TypeString = "Album";

                    foreach (var i in album.Images)
                        if (LargestImage == null || i.Width > LargestImage.Width) LargestImage = i;
                }
                else if (userDrop.Contains("playlist"))
                {
                    var playlist = await SpotifyAuth._spotify.GetPlaylistAsync(ID);

                    if (!String.IsNullOrEmpty(playlist.Owner.DisplayName))
                        toAdd.Artist = playlist.Owner.DisplayName;
                    else
                        toAdd.Artist = "Spotify";
                    toAdd.Title = playlist.Name;
                    toAdd.Type = QueueItemType.Playlist;
                    toAdd.TypeString = "Playlist";

                    foreach (var i in playlist.Images)
                        if (LargestImage == null || i.Width > LargestImage.Width) LargestImage = i;

                }
                else if (userDrop.Contains("episode"))
                {
                    MessageBox.Show("Podcasts are currently not supported, but may be supported in the future.");
                }
                else
                    return; //ignore bad links

                if (LargestImage != null && !File.Exists(imgDir))
                {
                    using (WebClient client = new WebClient())
                        client.DownloadFile(LargestImage.Url, imgDir);
                }
                toAdd.Image = imgDir;

                if (LargestImage == null)
                    toAdd.Image = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "spautofy.jpg");

                if (!ListBoxQueue.Contains(toAdd))
                {
                    ListBoxQueue.Add(toAdd);
                    return; //stop adding multiples!
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ERROR:  Not a valid Spotify link\n{ex.Message}\n{ex.StackTrace}");
            }
        }

    }
}
