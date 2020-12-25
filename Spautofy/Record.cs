using NAudio.Wave;
using SpotifyAPI.Web.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Spautofy
{
    class Record
    {
        public enum SpotifyType { track, album, playlist }

        public SpotifyType Type;
        public string ID;
        public string MultiID;
        public string OutputDir; //Where .mp3s are saved
        public string WorkingDir; //Where the temp.wav and album arts are saved
        public bool IsRecording;
        public bool IsReady;

        private string TempFilePath;
        private FullTrack _SingleSong;
        private List<SimpleTrack> _MultiSongs;
        private List<FullTrack> _PlaylistSongs;
        private List<SimpleArtist> _Artists;
        private SimpleAlbum _Album; //Used when recording single songs or playlists to get album art
        private FullAlbum _FullAlbum; //Used to hold album objects when recording full albums
        private FullPlaylist _FullPlaylist; //Used to hold playlist object when recording a playlist
        private int TotalNumberOfTracks; //Used for albums/playlists
        private int CurrentTrackIndex; //Used for albums/playlists
        private Device UserWebDevice; //Used for starting tracks

        public string Title;
        public string Album;
        public List<string> Artists;
        public int TrackNumber;
        public string ImagePath;
        public int DurationMS;
        public int ElapsedMS;


        public Record()
        {
            IsRecording = false;
        }

        public Record(string id, SpotifyType type) : this()
        {
            IsReady = false;
            ID = id;
            Type = type;
            Artists = new List<string>();
            OutputDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), "Spautofy");
            WorkingDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cache");
            if (!Directory.Exists(WorkingDir))
                Directory.CreateDirectory(WorkingDir);
            //GetDevice();
            Task.Run(async () => await GetData());
            Task.Run(async () => await GetDevice());
        }

        private async Task GetDevice()
        {
            UserWebDevice = null;
            var devices = await SpotifyAuth._spotify.GetDevicesAsync();
            if (devices == null || devices.Devices == null)
            {
                Task.Run(() => MessageBox.Show("Unable to find a Spotify device to play on\nGetDevices did not return anything\nMake sure spotify web page is still open, and pause/play a song and try again"));
                //return false;
            }
            else
            {
                try
                {
                    foreach (var d in devices.Devices)
                    {
                        if (d.Name.ToLower().Contains("web"))
                            UserWebDevice = d;
                    }
                }
                catch (Exception ex)
                {
                    Task.Run(() => MessageBox.Show("Error selecting web device to play on.  Try again?"));
                }
            }
            if (UserWebDevice == null)
            {
                Task.Run(() => MessageBox.Show($"Unable to find a Spotify device to play on\nActiveDevice was null"));
                //return false;
            }

        }

        //Waits for everything to be set up, stops UI thread from freezing while downloading album art setting data
        public async Task<bool> WaitForReady()
        {
            while (!IsReady)
                await Task.Delay(500);
            return true;
        }

        //A wrapper that can be called from in here to update the cover art, or called from outside to update and return the cover art
        public async Task<string> GetCoverArt()
        {
            if (Type == SpotifyType.track)
                ImagePath = await DownloadLargestCoverArt(_Album.Images);
            else if (Type == SpotifyType.album)
                ImagePath = await DownloadLargestCoverArt(_FullAlbum.Images);
            else if (Type == SpotifyType.playlist)
                ImagePath = await DownloadLargestCoverArt(_SingleSong.Album.Images);
            return ImagePath;
        }

        /// <summary>
        /// Removes illegal characters from a file or folder name
        /// </summary>
        /// <param name="filename"></param>
        private string RemoveIllegalCharacters(string name)
        {
            name = name.Replace('/', '-');
            name = name.Replace('\\', '-');
            name = name.Replace(':', '-');
            name = name.Replace('*', '_');
            name = name.Replace('\"', '_');
            name = name.Replace('<', '_');
            name = name.Replace('>', '_');
            name = name.Replace('|', '_');

            return name;
        }

        private async Task GetData()
        {
            if (Type == SpotifyType.track)
            {
                try
                {
                    _SingleSong = await SpotifyAuth._spotify.GetTrackAsync(ID);
                    Title = _SingleSong.Name;
                    _Artists = _SingleSong.Artists;
                    DurationMS = _SingleSong.DurationMs;

                    foreach (var artist in _Artists)
                        Artists.Add(artist.Name);

                    _Album = _SingleSong.Album;
                    Album = _Album.Name;

                    OutputDir = Path.Combine(OutputDir, "Tracks", RemoveIllegalCharacters(String.Join(" & ", Artists)));
                    if (!Directory.Exists(OutputDir))
                        Directory.CreateDirectory(OutputDir);
                }
                catch (Exception ex)
                {
                    Task.Run(() => MessageBox.Show($"Error while setting data for the track:\n{ex.Message}\n{ex.StackTrace}"));
                }
            }
            else if (Type == SpotifyType.album)
            {
                try
                {
                    MultiID = ID;
                    _FullAlbum = await SpotifyAuth._spotify.GetAlbumAsync(ID);
                    Album = _FullAlbum.Name;
                    _Artists = _FullAlbum.Artists;

                    foreach (var artist in _Artists)
                        Artists.Add(artist.Name);

                    _MultiSongs = _FullAlbum.Tracks.Items;
                    TotalNumberOfTracks = _MultiSongs.Count();
                    CurrentTrackIndex = 0;

                    OutputDir = Path.Combine(OutputDir, "Albums", RemoveIllegalCharacters(Album));
                    if (!Directory.Exists(OutputDir))
                        Directory.CreateDirectory(OutputDir);
                }
                catch (Exception ex)
                {
                    Task.Run(() => MessageBox.Show($"Error while setting data for the album:\n{ex.Message}\n{ex.StackTrace}"));
                }
            }
            else if (Type == SpotifyType.playlist) //set the album, artists when saving or something
            {
                try
                {
                    MultiID = ID;
                    _FullPlaylist = await SpotifyAuth._spotify.GetPlaylistAsync(ID);
                    //Album = _FullAlbum.Name;

                    _PlaylistSongs = new List<FullTrack>();
                    foreach (var song in _FullPlaylist.Tracks.Items)
                        _PlaylistSongs.Add(song.Track);

                    TotalNumberOfTracks = _PlaylistSongs.Count();
                    CurrentTrackIndex = 0;

                    OutputDir = Path.Combine(OutputDir, "Playlists", RemoveIllegalCharacters(_FullPlaylist.Name));
                    if (!Directory.Exists(OutputDir))
                        Directory.CreateDirectory(OutputDir);
                }
                catch (Exception ex)
                {
                    Task.Run(() => MessageBox.Show($"Error while setting data for the playlist:\n{ex.Message}\n{ex.StackTrace}"));
                }
            }

            IsReady = true;
        }

        //Used with playlists to set data to the next song
        public async Task<bool> LoadNextSongPlaylist()
        {
            try
            {
                if (CurrentTrackIndex < TotalNumberOfTracks)
                {
                    _SingleSong = _PlaylistSongs[CurrentTrackIndex];
                    CurrentTrackIndex += 1;
                }
                else
                    return false;

                Title = _SingleSong.Name;
                Album = _SingleSong.Album.Name;
                ID = _SingleSong.Id;
                DurationMS = _SingleSong.DurationMs;
                Artists.Clear();
                foreach (var artist in _SingleSong.Artists)
                    Artists.Add(artist.Name);
                await GetCoverArt();

                return true;
            }
            catch (Exception ex)
            {
                Task.Run(() => MessageBox.Show($"Error while loading the next track in the playlist:\n{ex.Message}\n{ex.StackTrace}"));
                return false;
            }
        }

        //Used with albums to set data to the next song
        public async Task<bool> LoadNextSongAlbum()
        {
            try
            {
                SimpleTrack nextSong = null;
                if (CurrentTrackIndex < TotalNumberOfTracks)
                {
                    nextSong = _MultiSongs[CurrentTrackIndex];
                    CurrentTrackIndex += 1;
                }
                else
                    return false;

                Title = nextSong.Name;
                ID = nextSong.Id;
                TrackNumber = nextSong.TrackNumber;
                DurationMS = nextSong.DurationMs;
                Artists.Clear();
                foreach (var artist in nextSong.Artists)
                    Artists.Add(artist.Name);

                return true;
            }
            catch (Exception ex)
            {
                Task.Run(() => MessageBox.Show($"Error while loading the next track in the album:\n{ex.Message}\n{ex.StackTrace}"));
                return false;
            }
        }

        private async Task<string> DownloadLargestCoverArt(List<Image> images)
        {
            try
            {
                string img = Path.Combine(WorkingDir, Type.ToString() + "-" + ID + ".jpg");
                if (File.Exists(img))
                {
                    ImagePath = img;
                    return img;
                }

                Image LargestImage = null;

                foreach (var i in images)
                    if (LargestImage == null || i.Width > LargestImage.Width) LargestImage = i;

                if (LargestImage != null)
                {
                    string outputImage = img;
                    using (WebClient client = new WebClient())
                        client.DownloadFile(LargestImage.Url, outputImage);
                    DisplayImage.img = outputImage;
                    ImagePath = outputImage;
                    return outputImage;
                }
            }
            catch (Exception ex)
            {
                Task.Run(() => MessageBox.Show($"Error downloading the cover art\n{ex.Message}\n{ex.StackTrace}"));
            }

            return null; //if there was an error
        }


        public async Task<bool> StartRecording()
        {
            if (UserWebDevice == null)
            {
                Task.Run(() => MessageBox.Show("Error trying to start recording, UserWebDevice is null!"));

                return false;
            }

            TempFilePath = Path.Combine(WorkingDir, "temp.wav");
            var capture = new WasapiLoopbackCapture();
            var writer = new WaveFileWriter(TempFilePath, capture.WaveFormat);
            int seconds = DurationMS / 1000 + 1;

            var res = await SpotifyAuth._spotify.ResumePlaybackAsync(UserWebDevice.Id, "", new List<string>() { "spotify:track:" + ID }, "", 0);
            if (res.Error != null)
                Task.Run(() => MessageBox.Show($"Spotify API error: {res.Error.Message}\n{res.Error.Status}"));

            capture.DataAvailable += (s, a) =>
            {
                writer.Write(a.Buffer, 0, a.BytesRecorded);
                if (writer.Position > capture.WaveFormat.AverageBytesPerSecond * seconds)
                    capture.StopRecording();
            };

            capture.RecordingStopped += (s, a) =>
            {
                IsRecording = false;
                writer.Dispose();
                writer = null;
                capture.Dispose();
            };

            IsRecording = true;
            //StartStopwatch();
            capture.StartRecording();
            while (capture.CaptureState != NAudio.CoreAudioApi.CaptureState.Stopped)
            {
                await Task.Delay(500);
            }
            
            var pause = await SpotifyAuth._spotify.PausePlaybackAsync();
            if (pause.Error != null)
                Console.WriteLine(pause.Error.Message);

            return true;
        }

        public async Task TrimWavSilence()
        {
            string tempFilePath = TempFilePath; //temp.wav
            TempFilePath = Path.Combine(WorkingDir, "temp_trimmed.wav");
            if (File.Exists(tempFilePath))
            {
                //big thanks to https://markheath.net/post/trimming-wav-file-using-naudio
                using (AudioFileReader reader = new AudioFileReader(tempFilePath))
                {
                    if (reader.TotalTime > TimeSpan.FromMilliseconds(DurationMS))
                    {
                        int bytesPerMS = reader.WaveFormat.AverageBytesPerSecond / 1000;
                        //int startTrim = (int)(reader.GetSilenceDuration(AudioFileReaderExt.SilenceLocation.Start).TotalMilliseconds + 5) * bytesPerMS;
                        //if (startTrim < 0) startTrim = 0;
                        //int startPos = startTrim - (startTrim % reader.WaveFormat.BlockAlign);
                        //int endTrim = (int)(reader.GetSilenceDuration(AudioFileReaderExt.SilenceLocation.End).TotalMilliseconds + 5) * bytesPerMS;
                        int endTrim = ((int)reader.TotalTime.TotalMilliseconds - DurationMS) * bytesPerMS;
                        int endPos = (int)reader.Length - (endTrim - (endTrim % reader.WaveFormat.BlockAlign));
                        byte[] buffer = new byte[1024];

                        //reader.Position = startPos;
                        using (WaveFileWriter writer = new WaveFileWriter(TempFilePath, reader.WaveFormat))
                        {
                            while (reader.Position < endPos)
                            {
                                int bytesRequired = (int)(endPos - reader.Position);
                                if (bytesRequired > 0)
                                {
                                    int bytesToRead = Math.Min(bytesRequired, buffer.Length);
                                    int bytesRead = reader.Read(buffer, 0, bytesToRead);
                                    if (bytesRead > 0)
                                    {
                                        writer.Write(buffer, 0, bytesRead);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        TempFilePath = Path.Combine(WorkingDir, "temp.wav");
                        return;
                    }
                }
            }
            else
                Console.WriteLine("WAV FILE NOT FOUND???");
        }

        public async Task ConvertToMP3()
        {
            string mp3 = Path.Combine(OutputDir, Title + ".mp3");
            int waitSec = 60;
            while (!File.Exists(TempFilePath) && waitSec > 0)
            {
                await Task.Delay(1000);
                waitSec -= 1;
            }
            Codec.WaveToMP3(TempFilePath, mp3, 320);
            TempFilePath = mp3;
        }

        public async Task AddID3Tags()
        {
            try
            {
                if (File.Exists(Path.Combine(TempFilePath)))
                {
                    TagLib.File f = TagLib.File.Create(Path.Combine(TempFilePath));
                    f.Tag.Title = Title;
                    f.Tag.Performers = Artists.ToArray();
                    f.Tag.Album = Album;
                    if (Type == SpotifyType.album)
                        f.Tag.Track = (uint)TrackNumber;
                    f.Tag.Pictures = new TagLib.IPicture[]
                    {
                        new TagLib.Picture(new TagLib.ByteVector((byte[])new System.Drawing.ImageConverter().ConvertTo(System.Drawing.Image.FromFile(ImagePath), typeof(byte[]))))
                        {
                            Type = TagLib.PictureType.FrontCover,
                            Description = "Cover",
                            MimeType = System.Net.Mime.MediaTypeNames.Image.Jpeg
                        }
                    };
                    f.Save();
                }
                else
                    Console.WriteLine("MP3 NOT FOUND???");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
            }
        }

    }
}
