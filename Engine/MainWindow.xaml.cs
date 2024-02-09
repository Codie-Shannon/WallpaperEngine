using Microsoft.Win32;
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
using GleamTech.VideoUltimate;
using System.IO;
using System.Windows.Threading;

namespace Engine
{
    public partial class MainWindow : Window
    {
        //Variables
        //Wallpaper Window
        public static WallpaperWindow WallpaperRef = new WallpaperWindow();
        //File Browser
        OpenFileDialog openFileDialog;
        string InitialDirectory = "shell:MyComputerFolder";
        string str_Importables = "Media (*.jpeg, *.jpg, *.png, *.tiff, *.psd, *.ai, *.raw, *.gif, *.avchd, *.avi, *.flv, *.mkv, *.mov, *.mp4, *.webm, *.wmv)|*.jpeg; *.jpg; *.png; *.tiff; *.psd; *.ai; *.raw; *.gif; *.JPEG; *.JPG; *.PNG; *.TIFF; *.PSD; *.AI; *.RAW; *.GIF; *.avchd; *.avi; *.flv; *.mkv; *.mov; *.mp4; *.webm; *.wmv; *.AVCHD; *.AVI; *.FLV; *.MKV; *.MOV; *.MP4; *.WEBM; *.WMV";
        //Monitor Count
        int monitorcounter;
        DispatcherTimer dispatcherTimer;
        bool[] isSet = new bool[] { false, false };
        //Other
        string[] VideoFormats = new string[] { "avchd", "avi", "flv", "mkv", "mov", "mp4", "webm", "wmv" };

        public MainWindow()
        {
            InitializeComponent();

            //Setup Window
            Setup();
        }

        public void Setup()
        {
            //Disable Wallpaper Button
            btnWallpaper.IsEnabled = false;

            if (System.Windows.Forms.Screen.AllScreens.Length == 1)
            {
                if (Properties.Settings.Default.isSet == true)
                {
                    //Load Wallpaper Information
                    LoadInfo();
                }
            }
            else
            {
                //Disable Browse Button
                btnBrowse.IsEnabled = false;

                //Change Wallpaper Availability Message
                tbWallpaperAvailability.Text = "Monitor Count Unsupported";

                //Show Message
                tbWallpaperAvailability.Visibility = Visibility.Visible;

                //Hide Wallpapers
                meDisplayWallpaper.Visibility = Visibility.Collapsed;
            }

            //Check Monitor Count Every 5 Seconds
            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(CheckMonitorCount_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 5);
            dispatcherTimer.Start();
        }

        private void CheckMonitorCount_Tick(object sender, EventArgs e)
        {
            //Get Monitor Count
            monitorcounter = System.Windows.Forms.Screen.AllScreens.Length;

            if(monitorcounter == 1 && isSet[0] == false)
            {
                //Reset Message
                tbWallpaperAvailability.Text = "No Wallpaper Available";

                //Enable Browse Button
                btnBrowse.IsEnabled = true;

                //Load Info
                LoadInfo();

                //Set Bools
                isSet[0] = true;
                isSet[1] = false;
            }
            else if (monitorcounter > 1 && isSet[1] == false)
            {
                //Disable Browse Button
                btnBrowse.IsEnabled = false;

                //Change Wallpaper Availability Message
                tbWallpaperAvailability.Text = "Monitor Count Unsupported";

                //Show Message
                tbWallpaperAvailability.Visibility = Visibility.Visible;

                //Hide Wallpapers
                HideWallpaper();
                meDisplayWallpaper.Visibility = Visibility.Collapsed;

                //Set Bools
                isSet[1] = true;
                isSet[0] = false;
            }
        }

        #region Main Bar
        private void StackPanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }

        private void btnDetails_Click(object sender, RoutedEventArgs e)
        {
            //Hide Wallpaper Tab
            btnWallpaper.IsEnabled = true;
            WallpaperTab.Visibility = Visibility.Hidden;

            //Show Details Tab
            btnDetails.IsEnabled = false;
            DetailsTab.Visibility = Visibility.Visible;
        }

        private void btnWallpaper_Click(object sender, RoutedEventArgs e)
        {
            //Hide Details Tab
            btnDetails.IsEnabled = true;
            DetailsTab.Visibility = Visibility.Hidden;

            //Hide Wallpaper Tab
            btnWallpaper.IsEnabled = false;
            WallpaperTab.Visibility = Visibility.Visible;
        }

        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            SystemCommands.MinimizeWindow(this);
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            //Refresh Desktop
            WallpaperWindow.SystemParametersInfo(WallpaperWindow.SPI_SETDESKWALLPAPER, 0, null, WallpaperWindow.SPIF_UPDATEINIFILE);
            
            //Close Application
            Application.Current.Shutdown();
        }
        #endregion Main Bar

        #region Wallpaper Body
        private void meDisplayWallpaper_MediaEnded(object sender, RoutedEventArgs e)
        {
            //Reset Position
            meDisplayWallpaper.Position = TimeSpan.Zero;

            //Play Video
            meDisplayWallpaper.Play();
        }

        private void meDisplayWallpaper_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            //Reset To Defaults and Display Error Message
            ResetDefaults();
        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            //Creating Open File Dialog
            openFileDialog = new OpenFileDialog();

            //Assign File Parameters
            openFileDialog.InitialDirectory = InitialDirectory;
            openFileDialog.Filter = str_Importables;
            openFileDialog.RestoreDirectory = true;

            if (openFileDialog.ShowDialog() == true)
            {
                //Save Wallpaper Information
                SaveInfo(openFileDialog.FileName);

                //Load Wallpaper Information
                LoadInfo();
            }
        }
        #endregion Wallpaper Body

        #region Main Methods
        public void SaveInfo(string FileName)
        {
            try
            {
                //Save File Name
                Properties.Settings.Default.FileName = Path.GetFileNameWithoutExtension(FileName);

                //Save Path
                Properties.Settings.Default.Path = openFileDialog.FileName;

                //Save Format
                Properties.Settings.Default.Format = Path.GetExtension(FileName).ToUpper().Replace(".", "");

                //Save File Size
                Properties.Settings.Default.FileSize = GetSize(new FileInfo(FileName).Length.ToString());

                //Create Video Reader
                VideoFrameReader VideoReader = new VideoFrameReader(FileName);

                //Save Resolution
                Properties.Settings.Default.Resolution = VideoReader.Width + " x " + VideoReader.Height;

                //Save Duration
                if (VideoFormats.Contains(Properties.Settings.Default.Format.ToLower()))
                    Properties.Settings.Default.Duration = VideoReader.Duration.ToString(@"hh\:mm\:ss");
                else
                    Properties.Settings.Default.Duration = "N / A";

                //Dispose of Video Reader
                VideoReader.Dispose();

                //Save Is Set Value
                Properties.Settings.Default.isSet = true;

                //Save Settings
                Properties.Settings.Default.Save();
            }
            catch
            {
                //Reset To Defaults and Display Error Message
                ResetDefaults();
            }
        }

        public void LoadInfo()
        {
            try
            {
                // Wallpaper Page
                // =====================================================================================
                //Load Title
                tbWallpaperTitle.Text = Properties.Settings.Default.FileName;

                //Hide Wallpaper Availability Message
                tbWallpaperAvailability.Visibility = Visibility.Collapsed;

                //Show Display Wallpaper
                meDisplayWallpaper.Visibility = Visibility.Visible;

                //Load Display Wallpaper
                meDisplayWallpaper.Source = new Uri(Properties.Settings.Default.Path, UriKind.Absolute);

                //Play Display Wallpaper
                meDisplayWallpaper.Play();

                //Reset Display Path Color
                tbDisplayPath.Foreground = Brushes.White;

                //Load Display Path
                tbDisplayPath.Text = Properties.Settings.Default.Path;


                // Details Page
                // ====================================================================================
                //Set File Name
                tbFileName.Text = Properties.Settings.Default.FileName;

                //Set Path
                tbPath.Text = Properties.Settings.Default.Path;

                //Set Format
                tbFormat.Text = Properties.Settings.Default.Format;

                //Set File Size
                tbFileSize.Text = Properties.Settings.Default.FileSize;

                //Set Resolution
                tbResolution.Text = Properties.Settings.Default.Resolution;

                //Set Duration
                tbDuration.Text = Properties.Settings.Default.Duration;


                //Wallpaper Window
                // ====================================================================================
                //Parent Wallpaper Window
                WallpaperWindow.ParentwWallpaper();

                //Change Wallpaper
                WallpaperWindow.ChangeWallpaper();

                ShowWallpaper();
            }
            catch
            {
                //Reset To Defaults and Display Error Message
                ResetDefaults();
            }
        }

        public void ResetDefaults()
        {
            //Hide Wallpaper Window
            HideWallpaper();

            //Hide Display Wallpaper
            meDisplayWallpaper.Visibility = Visibility.Collapsed;

            //Show Wallpaper Availability Message
            tbWallpaperAvailability.Visibility = Visibility.Visible;

            //Reset Text
            tbWallpaperTitle.Text = "Wallpaper Name";
            tbDisplayPath.Text = "An error occured please select another wallpaper...";
            tbDisplayPath.Foreground = Brushes.DarkRed;
            tbFileName.Text = "N / A";
            tbPath.Text = "N / A";
            tbFormat.Text = "N / A";
            tbFileSize.Text = "N / A";
            tbResolution.Text = "N / A";
            tbDuration.Text = "N / A";

            //Reset Settings
            Properties.Settings.Default.FileName = "";
            Properties.Settings.Default.Path = "";
            Properties.Settings.Default.Format = "";
            Properties.Settings.Default.FileSize = "";
            Properties.Settings.Default.Resolution = "";
            Properties.Settings.Default.Duration = "";
            Properties.Settings.Default.isAnimated = false;
            Properties.Settings.Default.isSet = false;

            //Save Settings
            Properties.Settings.Default.Save();
        }

        string GetSize(string str_bytesize)
        {
            if (str_bytesize != "")
            {
                float float_bytesize = Convert.ToInt32(str_bytesize);

                if (float_bytesize / 1024 / 1024 / 1024 / 1024 >= 1)
                    return "" + Convert.ToInt32(float_bytesize / 1024 / 1024 / 1024 / 1024) + " TB";
                else if (float_bytesize / 1024 / 1024 / 1024 >= 1)
                    return "" + Convert.ToInt32(float_bytesize / 1024 / 1024 / 1024) + " GB";
                else if (float_bytesize / 1024 / 1024 >= 1)
                    return "" + Convert.ToInt32(float_bytesize / 1024 / 1024) + " MB";
                else if (float_bytesize / 1024 >= 1)
                    return "" + Convert.ToInt32(float_bytesize / 1024) + " KB";
            }

            return "No File Size Available";
        }
        #endregion Main Methods

        public void ShowWallpaper()
        {
            //Show Wallpaper Window
            WallpaperRef.Show();

            //Refresh Desktop
            WallpaperWindow.SystemParametersInfo(WallpaperWindow.SPI_SETDESKWALLPAPER, 0, null, WallpaperWindow.SPIF_UPDATEINIFILE);
        }

        public void HideWallpaper()
        {
            //Show Wallpaper Window
            WallpaperRef.Hide();

            //Refresh Desktop
            WallpaperWindow.SystemParametersInfo(WallpaperWindow.SPI_SETDESKWALLPAPER, 0, null, WallpaperWindow.SPIF_UPDATEINIFILE);
        }
    }
}
