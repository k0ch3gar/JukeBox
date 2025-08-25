using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.IO;

namespace Jukebox_Mascot
{
    /// <summary>
    /// Interaction logic for Jukebox.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private NotifyIcon TRAY_ICON;

        private bool PLAY_INTRO_ON_NEW_SONG = true;
        private const int DEFAULT_WIDTH = 300;
        private const int DEFAULT_HEIGHT = 300;
        private int FRAME_WIDTH_JUKEBOX = 300;
        private int FRAME_HEIGHT_JUKEBOX = 450;
        private int FRAME_WIDTH = 300;
        private int FRAME_HEIGHT = 300;
        private string START_CHAR = "RiceShower";

        private int JUKEBOX_FUNNY_FRAME_COUNT = 17;
        private int MUSIC_NOTE_FRAME = 123;
        private int INTRO_FRAME_COUNT = 52;
        private int DANCE_FRAME_COUNT = 187;
        private int JUKEBOX_FRAME_COUNT = 22;

        private int SPRITE_COLUMN = 5;

        private int CURRENT_JUKEBOXF_FRAME = 0;
        private int CURRENT_INTRO_FRAME = 0;
        private int CURRENT_DANCE_FRAME = 0;
        private int CURRENT_JUKEBOX_FRAME = 0;
        private int CURRENT_MUSIC_NOTE_FRAME = 0;

        private int FRAME_RATE = 31;

        private BitmapImage MUSIC_NOTE_SHEET;
        private BitmapImage JUKEBOX_FUNNY_SHEET;
        private BitmapImage INTRO_SHEET;
        private BitmapImage DANCE_SHEET;
        private BitmapImage JUKEBOX_SHEET;

        private DispatcherTimer CLOSE_TIMER;
        private DispatcherTimer MASTER_TIMER;
        private DispatcherTimer SCROLL_TIMER;

        private bool IS_INTRO = true;
        private bool IS_INTRO_JUKEBOX = true;
        private bool forward = true;
        private bool ALLOW_RANDOM_MASCOT = true;

        private double SCROLL_POS;

        private List<string> CHARACTER_FOLDERS = new List<string>();

        private MediaPlayer PLAYER;
        private List<string> MUSIC_FILES = new List<string>();
        private int CURRENT_TRACK_INDEX = 0;
        private bool IS_RANDOM = false;

        private BitmapImage LoadSprite(string filefolder,string fileName)
        {
            string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SpriteSheet","Characters",filefolder ,fileName);

            if (!File.Exists(path))
            {
                System.Windows.MessageBox.Show(
                  $"Missing sprite file / Wrong File name Format:\n{fileName}\n\nPath: {path}",
                  "Sprite Load Error",
                  MessageBoxButton.OK,
                  MessageBoxImage.Error
                );
                System.Windows.Application.Current.Shutdown();
            }

            var image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri(path);
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.EndInit();
            image.Freeze();
            return image;
        }
        private BitmapImage LoadJukeSprite(string filefolder, string fileName)
        {
            string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SpriteSheet", filefolder, fileName);

            if (!File.Exists(path))
            {
                System.Windows.MessageBox.Show(
                  $"Missing sprite file / Wrong File name Format:\n{fileName}\n\nPath: {path}",
                  "Sprite Load Error",
                  MessageBoxButton.OK,
                  MessageBoxImage.Error
                );
                System.Windows.Application.Current.Shutdown();
            }

            var image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri(path);
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.EndInit();
            image.Freeze();
            return image;
        }

        private void LoadSpritesSheet()
        {
            SpriteImage.Source = null;
            JUKEBOX_SHEET = LoadJukeSprite("Jukebox","jukebox.png");
            INTRO_SHEET = LoadSprite(START_CHAR,"intro.png");
            DANCE_SHEET = LoadSprite(START_CHAR,"dance.png");
            MUSIC_NOTE_SHEET = LoadJukeSprite("Jukebox","music_note.png");
            JUKEBOX_FUNNY_SHEET = LoadJukeSprite("Jukebox","music_box_goofy.png");
        }


        private void LoadCharacterFolders()
        {
            string spriteRoot = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SpriteSheet", "Characters");

            if (!Directory.Exists(spriteRoot))
            {
                System.Windows.MessageBox.Show("SpriteSheet folder is missing!","Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                System.Windows.Application.Current.Shutdown();
            }

            CHARACTER_FOLDERS = Directory.GetDirectories(spriteRoot)
                                         .Select(Path.GetFileName)
                                         .ToList();
        }


        private int PlayAnimation(BitmapImage sheet,int currentFrame,int frameCount,int frameWidth,int frameHeight,System.Windows.Controls.Image targetImage,bool reverse = false)
        {
            if (sheet == null)
                return currentFrame;

            int x = (currentFrame % SPRITE_COLUMN) * frameWidth;
            int y = (currentFrame / SPRITE_COLUMN) * frameHeight;

            if (x + frameWidth > sheet.PixelWidth || y + frameHeight > sheet.PixelHeight)
                return currentFrame;

            targetImage.Source = new CroppedBitmap(sheet, new Int32Rect(x, y, frameWidth, frameHeight));

            if (!reverse)
            {             
                return (currentFrame + 1) % frameCount;
            }
            else
            {
                if (forward)
                {
                    currentFrame++;
                    if (currentFrame >= frameCount - 1) forward = false;
                }
                else
                {
                    currentFrame--;
                    if (currentFrame <= 0) forward = true;
                }
                return currentFrame;
            }
        }

        private void InitializeAnimations()
        {
            MASTER_TIMER = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(FRAME_RATE) };
            MASTER_TIMER.Tick += (s, e) =>
            {
                if (IS_INTRO)
                {
                    CURRENT_INTRO_FRAME = PlayAnimation(
                        INTRO_SHEET, CURRENT_INTRO_FRAME, INTRO_FRAME_COUNT,
                        FRAME_WIDTH, FRAME_HEIGHT, SpriteImage);

                    if (CURRENT_INTRO_FRAME == 0)
                    {
                        IS_INTRO_JUKEBOX = false;
                        IS_INTRO = false;
                        JukeBoxSprite.Source = null;
                    }
                }
                if (IS_INTRO_JUKEBOX)
                {
                    CURRENT_JUKEBOX_FRAME = PlayAnimation(JUKEBOX_SHEET, CURRENT_JUKEBOX_FRAME, JUKEBOX_FRAME_COUNT,
                        FRAME_WIDTH_JUKEBOX, FRAME_HEIGHT_JUKEBOX, JukeBoxSprite);
                }

                if (!IS_INTRO)
                {
                    CURRENT_JUKEBOXF_FRAME = PlayAnimation(JUKEBOX_FUNNY_SHEET, CURRENT_JUKEBOXF_FRAME, JUKEBOX_FUNNY_FRAME_COUNT,
                        DEFAULT_WIDTH, DEFAULT_HEIGHT, JukeBoxSpriteFunny, reverse: true);

                    CURRENT_DANCE_FRAME = PlayAnimation(DANCE_SHEET, CURRENT_DANCE_FRAME, DANCE_FRAME_COUNT,
                        FRAME_WIDTH, FRAME_HEIGHT, SpriteImage);
                }

                CURRENT_MUSIC_NOTE_FRAME = PlayAnimation(MUSIC_NOTE_SHEET, CURRENT_MUSIC_NOTE_FRAME, MUSIC_NOTE_FRAME,
                    DEFAULT_WIDTH, DEFAULT_HEIGHT, MusicNote);
            };
            MASTER_TIMER.Start();
        }

        private void InitializeMusic()
        {
            PLAYER = new MediaPlayer();
            string musicDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Music");

            if (!Directory.Exists(musicDir))
                Directory.CreateDirectory(musicDir);

            MUSIC_FILES = Directory.GetFiles(musicDir, "*.mp3").ToList();

            if (MUSIC_FILES.Count == 0)
            {
                System.Windows.MessageBox.Show("No music files found in Music folder!", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            CURRENT_TRACK_INDEX = 0;
            LoadTrack(CURRENT_TRACK_INDEX);

            PLAYER.MediaEnded += (s, e) => { NextTrack(); };
            PLAYER.Play();
        }

        private void LoadTrack(int index)
        {
            if (index < 0 || index >= MUSIC_FILES.Count)
                return;

            string filePath = MUSIC_FILES[index];
            PLAYER.Open(new Uri(filePath));

            string songName = System.IO.Path.GetFileNameWithoutExtension(filePath);
            ScrollingText.Text = $"🎵 Now Playing: {songName} 🎵";
            ReopenScrollingBorder();

            if (ALLOW_RANDOM_MASCOT)
            {
                SwitchToRandomCharacter();
            }
            if (PLAY_INTRO_ON_NEW_SONG)
            {
                JukeBoxSprite.Source = null;
                IS_INTRO = true;
                CURRENT_INTRO_FRAME = 0;
                CURRENT_JUKEBOX_FRAME = 0;
                JukeBoxSprite.Source = null; 
            }
        }


        private void PlayMusic() => PLAYER.Play();
        private void PauseMusic() => PLAYER.Pause();

        private void NextTrack()
        {
            if (IS_RANDOM)
            {
                var rand = new Random();
                CURRENT_TRACK_INDEX = rand.Next(MUSIC_FILES.Count);
            }
            else
            {
                CURRENT_TRACK_INDEX = (CURRENT_TRACK_INDEX + 1) % MUSIC_FILES.Count;
            }

            LoadTrack(CURRENT_TRACK_INDEX);
            PLAYER.Play();
        }

        private void ReopenScrollingBorder()
        {
            SCROLL_TIMER?.Stop();
            CLOSE_TIMER?.Stop();

            ScrollingText.Visibility = Visibility.Collapsed;
            ScrollingBorder.Width = 0;
            ScrollingBorder.Visibility = Visibility.Collapsed;

            DispatcherTimer openTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(15) };
            openTimer.Tick += (s, e) =>
            {
                if (ScrollingBorder.Width == 0)
                    ScrollingBorder.Visibility = Visibility.Visible;

                if (ScrollingBorder.Width < 250)
                {
                    ScrollingBorder.Width += 10;
                }
                else
                {
                    openTimer.Stop();
                    string filePath = MUSIC_FILES[CURRENT_TRACK_INDEX];
                    string songName = System.IO.Path.GetFileNameWithoutExtension(filePath);
                    ScrollingText.Text = $"🎵 Now Playing: {songName} 🎵";
                    ScrollingText.Visibility = Visibility.Visible;
                    StartScrolling();
                }
            };
            openTimer.Start();
        }

        private void StartScrolling()
        {
            SCROLL_TIMER?.Stop();
            SCROLL_POS = (int)ScrollingBorder.Width;
            var transform = new TranslateTransform(SCROLL_POS, 0);
            ScrollingText.RenderTransform = transform;

            SCROLL_TIMER = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(30) };
            SCROLL_TIMER.Tick += (s, e) =>
            {
                SCROLL_POS -= 2;
                transform.X = SCROLL_POS;

                if (SCROLL_POS < -ScrollingText.ActualWidth)
                {
                    SCROLL_TIMER.Stop();
                    StartClosingAnimation();
                }
            };
            SCROLL_TIMER.Start();
        }

        private void StartClosingAnimation()
        {
            CLOSE_TIMER = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(15) };
            CLOSE_TIMER.Tick += (s, e) =>
            {
                if (ScrollingBorder.Width > 0)
                    ScrollingBorder.Width -= 10;
                else
                {
                    CLOSE_TIMER.Stop();
                    ScrollingBorder.Visibility = Visibility.Collapsed;
                }
            };
            CLOSE_TIMER.Start();
        }


        private void ResetApp()
        {
            TRAY_ICON.Visible = false;
            string exePath = Process.GetCurrentProcess().MainModule.FileName;
            Process.Start(exePath);
            System.Windows.Application.Current.Shutdown();
        }

        private void CloseApp()
        {
            TRAY_ICON.Visible = false;
            TRAY_ICON?.Dispose();
            MASTER_TIMER.Stop();
            System.Windows.Application.Current.Shutdown();
        }
        private void SetupTrayIcon()
        {
            TRAY_ICON = new NotifyIcon();
            TRAY_ICON.Icon = new Icon("icon2.ico");
            TRAY_ICON.Visible = true;
            TRAY_ICON.Text = "Jukebox";

            var menu = new ContextMenuStrip();
            menu.Items.Add("Play", null, (s, e) => PlayMusic());
            menu.Items.Add("Pause", null, (s, e) => PauseMusic());
            menu.Items.Add("Next Track", null, (s, e) => NextTrack());

            var randomChar = new ToolStripMenuItem("Random Characters") { CheckOnClick = true };
            randomChar.CheckedChanged += (s, e) =>
            {
                ALLOW_RANDOM_MASCOT = randomChar.Checked;
            };

            var randomItem = new ToolStripMenuItem("Random Music") { CheckOnClick = true };
            randomItem.CheckedChanged += (s, e) =>
            { 
                IS_RANDOM = randomItem.Checked;
            };

            menu.Items.Add(randomItem);
            menu.Items.Add(randomChar); 
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Reappear", null, (s, e) => ResetApp());
            menu.Items.Add("Close", null, (s, e) => CloseApp());

            TRAY_ICON.ContextMenuStrip = menu;
        }
        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }
        public MainWindow()
        {
            InitializeComponent();
            LoadMasterConfig();
            LoadConfigChar();
            this.ShowInTaskbar = false;
            SetupTrayIcon();
            LoadSpritesSheet();
            LoadCharacterFolders(); 
            InitializeAnimations();
            InitializeMusic();
        }
        private void LoadMasterConfig()
        {
            string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Config.txt");
            if (!File.Exists(path))
            {
                System.Windows.MessageBox.Show(
                   "Where the Fuu is the config file!?",
                   "No Config File",
                   MessageBoxButton.OK,
                   MessageBoxImage.Error
               );
            }

            foreach (var line in File.ReadAllLines(path))
            {
                if (string.IsNullOrWhiteSpace(line) || !line.Contains("="))
                    continue;

                var parts = line.Split('=');
                if (parts.Length != 2)
                    continue;

                string key = parts[0].Trim();
                string value = parts[1].Trim();


                switch (key.ToUpper())
                {
                    case "START_CHAR": START_CHAR = value; break;
                    case "ALLOW_RANDOM_MASCOT":
                        if (bool.TryParse(value, out bool boolValue))
                            ALLOW_RANDOM_MASCOT = boolValue;
                        break;
                }
            }
        }
        private void SwitchToRandomCharacter()
        {
            if (CHARACTER_FOLDERS == null || CHARACTER_FOLDERS.Count == 0)
            {
                System.Windows.MessageBox.Show(
                    "No character folders found in SpriteSheet!",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                return;
            }

            var rand = new Random();
            string randomChar = CHARACTER_FOLDERS[rand.Next(CHARACTER_FOLDERS.Count)];

            START_CHAR = randomChar; 
            LoadConfigChar();

            INTRO_SHEET = LoadSprite(randomChar, "intro.png");
            DANCE_SHEET = LoadSprite(randomChar, "dance.png");

            CURRENT_INTRO_FRAME = 0;
            CURRENT_DANCE_FRAME = 0;
            IS_INTRO = true;

            JukeBoxSprite.Source = null;
        }

        private void LoadConfigChar()
        {
            string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"SpriteSheet","Characters", START_CHAR, "config.txt");
            if (!File.Exists(path))
            {
                System.Windows.MessageBox.Show(
                   "Where the Fuu is the config file!?",
                   "No Config File",
                   MessageBoxButton.OK,
                   MessageBoxImage.Error
               );
            }

            foreach (var line in File.ReadAllLines(path))
            {
                if (string.IsNullOrWhiteSpace(line) || !line.Contains("="))
                    continue;

                var parts = line.Split('=');
                if (parts.Length != 2)
                    continue;

                string key = parts[0].Trim();
                string value = parts[1].Trim();

                if (!int.TryParse(value, out int intValue))
                    continue;

                switch (key.ToUpper())
                {
                    case "FRAME_WIDTH": FRAME_WIDTH = intValue; break;
                    case "FRAME_HEIGHT": FRAME_HEIGHT = intValue; break;
                    case "FRAME_RATE": FRAME_RATE = intValue; break;
                    case "INTRO_FRAME_COUNT": INTRO_FRAME_COUNT = intValue; break;
                    case "DANCE_FRAME_COUNT": DANCE_FRAME_COUNT = intValue; break;    
                }
            }
        }


    }
}
