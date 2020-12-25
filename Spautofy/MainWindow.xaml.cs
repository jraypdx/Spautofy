using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Spautofy
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            //this.ListBoxQueue.Drop += new DragEventHandler(ListBoxQueue_Drop);
            //this.ListBoxQueue.DragEnter += new DragEventHandler(ListBoxQueue_DragEnter);
        }


        public void ListBoxQueue_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.StringFormat))
                e.Effects = DragDropEffects.All;
            else if (e.Data.GetDataPresent(typeof(QueueItem)))
                e.Effects = DragDropEffects.Move;
            else
                e.Effects = DragDropEffects.Move;
        }


        private void ListBoxQueue_Drop(object sender, DragEventArgs e)
        {
            string userDrop = null;
            userDrop = (string)e.Data.GetData(DataFormats.StringFormat);
            if (userDrop != null)
                ((MainWindowViewModel)DataContext).ListBoxQueue_DragDrop(userDrop);
        }

    }
}
