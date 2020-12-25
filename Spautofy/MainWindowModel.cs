using DirectShowLib;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Spautofy
{
    class MainWindowModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        //public ObservableCollection<WaveOutCapabilities> AudioDeviceList;
        //public WaveOutCapabilities SelectedAudioDevice;
        public string NowPlayingOutput { get; set; }
        public float SystemVolumeProgressBar_Output { get; set; }
        public float NowPlayingProgressBar_Output { get; set; }
        public string SpotifyTextoutput { get; set; }
        public string DragDropSpotifyLinkBox { get; set; }
        public ObservableCollection<QueueItem> ListBoxQueue;
        public Visibility Queue_Visibility { get; set; }

        public MainWindowModel()
        {
            ListBoxQueue = new ObservableCollection<QueueItem>();
            Queue_Visibility = Visibility.Collapsed;
        }

        /*private ObservableCollection<WaveOutCapabilities> GetAudioOutDevices()
        {
            //var FullNames = DsDevice.GetDevicesOfCat(FilterCategory.AudioRendererCategory);
            var outList = new ObservableCollection<WaveOutCapabilities>();
            for (int n = -1; n < WaveOut.DeviceCount; n++)
            {
                outList.Add(WaveOut.GetCapabilities(n));
            }
            return outList;
        }*/
    }
}
