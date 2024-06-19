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
using System.Windows.Shapes;

namespace Apollo
{
    public partial class MapWindow : Window
    {
        private bool isDefaultMap = true;

        public MapWindow()
        {
            InitializeComponent();
            UpdateMapImage();
        }

        private void SwitchMap_Click(object sender, RoutedEventArgs e)
        {
            isDefaultMap = !isDefaultMap;

            UpdateMapImage();
        }

        private void UpdateMapImage()
        {
            string basicImage = "https://fortnite-api.com/images/map.png";
            string altImage = "https://fortnite-api.com/images/map_en.png";

            string imageUrl = isDefaultMap ? basicImage : altImage;

            BitmapImage bitmap = new BitmapImage(new Uri(imageUrl));
            mapImage.Source = bitmap;
        }
    }
}
