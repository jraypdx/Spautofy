using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spautofy
{
    enum QueueItemType
    {
        Track,
        Album,
        Playlist
    }

    class QueueItem : ViewModelBase
    {
        public string Artist { get; set; }
        public string Title { get; set; }
        public string Image { get; set; } //path to image
        public string TypeString { get; set; } //Displays the type in queue
        public QueueItemType Type { get; set; }
        public string Link;


        public QueueItem(string link)
        {
            Link = link;
        }

        public QueueItem(QueueItemType type, string artist, string title, string image)
        {
            Type = type;
            Artist = artist;
            Title = title;
            Image = image;
        }

        public string GetID()
        {
            return Link.Split('/').Last();
        }
    }
}
