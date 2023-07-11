using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;
using ICSharpCode.SharpZipLib.Zip;

namespace UniversalGameLauncher
{
    public partial class Application : Form
    {

        private DownloadProgressTracker _downloadProgressTracker;
        private WebClient _webClient;
        private ProgressBar extractionProgressBar;
        private Label extractionStatusLabel;


        public Version LocalVersion { get { return new Version(Properties.Settings.Default.VersionText); } }
        public Version OnlineVersion { get; private set; }

        private List<PatchNoteBlock> patchNoteBlocks = new List<PatchNoteBlock>();

        private bool _isReady;
        public bool IsReady
        {
            get
            {
                return _isReady;
            }
            set
            {
                _isReady = value;
                TogglePlayButton(value);
                InitializeFooter();
            }
        }

        public bool UpToDate { get { return LocalVersion == OnlineVersion; } }

        public Application()
        {
            InitializeComponent();
            int style = NativeWinAPI.GetWindowLong(this.Handle, NativeWinAPI.GWL_EXSTYLE);
            style |= NativeWinAPI.WS_EX_COMPOSITED;
            NativeWinAPI.SetWindowLong(this.Handle, NativeWinAPI.GWL_EXSTYLE, style);
        }

        private void OnLoadApplication(object sender, EventArgs e)
        {
            InitializeConstantsSettings();
            InitializeFiles();
            InitializeImages();
            FetchPatchNotes();
            InitializeVersionControl();

            IsReady = UpToDate;

            _downloadProgressTracker = new DownloadProgressTracker(50, TimeSpan.FromMilliseconds(500));

            if (!UpToDate && Constants.AUTOMATICALLY_BEGIN_UPDATING)
            {
                DownloadFile();
            }

            // Create an instance of the Extract class
            Extract extract = new Extract(this);

            // Subscribe to the ProgressChanged event
            extract.ProgressChanged += ExtractionProgressChanged;

            // Subscribe to the ExtractionCompleted event (if needed)
            extract.ExtractionCompleted += ExtractionCompleted;
        }

        private void InitializeConstantsSettings()
        {
            Name = Constants.GAME_TITLE;
            Text = Constants.LAUNCHER_NAME;
            SetUpButtonEvents();

            currentVersionLabel.Visible = Constants.SHOW_VERSION_TEXT;

        }

        private void InitializeFiles()
        {
            if (!Directory.Exists(Constants.DESTINATION_PATH))
            {
                Directory.CreateDirectory(Constants.DESTINATION_PATH);
            }
        }

        private void InitializeImages()
        {
            LoadApplicationIcon();
            navbarPanel.BackColor = Color.FromArgb(255, 0, 0, 0); // Make panel background semi-transparent
            logoPictureBox.SizeMode = PictureBoxSizeMode.CenterImage;
            closePictureBox.SizeMode = PictureBoxSizeMode.CenterImage; // Center the X icon
            minimizePictureBox.SizeMode = PictureBoxSizeMode.CenterImage; // Center the - icon
            try
            {
                logoPictureBox.Load(Constants.LOGO_URL);

                // Append a unique query parameter to the background image URL
                string backgroundImageUrl = Constants.BACKGROUND_URL + "?timestamp=" + DateTime.Now.Ticks;
                using (WebClient webClient = new WebClient())
                {
                    using (Stream stream = webClient.OpenRead(backgroundImageUrl))
                    {
                        BackgroundImage = Image.FromStream(stream);
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("The launcher was unable to retrieve some game images from the server! " + e, "Error");
            }
        }



        private void LoadApplicationIcon()
        {
            WebRequest request = (HttpWebRequest)WebRequest.Create(Constants.APPLICATION_ICON_URL);

            Bitmap bm = new Bitmap(32, 32);
            MemoryStream memStream;

            using (Stream response = request.GetResponse().GetResponseStream())
            {
                memStream = new MemoryStream();
                byte[] buffer = new byte[1024];
                int byteCount;

                do
                {
                    byteCount = response.Read(buffer, 0, buffer.Length);
                    memStream.Write(buffer, 0, byteCount);
                } while (byteCount > 0);
            }

            bm = new Bitmap(Image.FromStream(memStream));

            if (bm != null)
            {
                Icon = Icon.FromHandle(bm.GetHicon());
            }

        }

        private void InitializeVersionControl()
        {
            OnlineVersion = GetOnlineVersion();
            currentVersionLabel.Text = "Current Version: " + LocalVersion;
            updatedVersionLabel.Text = "New Version: " + OnlineVersion;


            Console.WriteLine("We are on version " + LocalVersion + " and the online version is " + OnlineVersion);
        }

        private void InitializeFooter()
        {
            if (IsReady)
            {
                updateProgressBar.Visible = false;
                updatedVersionLabel.Visible = false;
                extractionProgressBar = new ProgressBar();
                extractionProgressBar.Visible = false;
                extractionStatusLabel = new Label();
                extractionStatusLabel.Visible = false;

            }
            else
            {
                updateProgressBar.Visible = true;
                updatedVersionLabel.Visible = true;
                extractionProgressBar = new ProgressBar();
                extractionProgressBar.Visible = true;
                extractionStatusLabel = new Label();
                extractionStatusLabel.Visible = true;
            }
        }

        private Version GetOnlineVersion()
        {
            try
            {
                string onlineVersion = new WebClient().DownloadString(Constants.VERSION_URL);
                Console.WriteLine(LocalVersion >= new Version(onlineVersion));
                Version.TryParse(onlineVersion, out Version result);
                return result;
            }
            catch
            {
                MessageBox.Show("The launcher was unable to read the current client version from the server!", "Fatal error");
                return null;
            }
        }

        private void OnClickPlay(object sender, EventArgs e)
        {
            if (IsReady)
            {
                LaunchGame();
            }
            else
            {
                DownloadFile();
            }
        }

        private void DownloadFile()
        {
            using (_webClient = new WebClient())
            {
                _webClient.DownloadProgressChanged += OnDownloadProgressChanged;
                _webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(OnDownloadCompleted);
                _webClient.DownloadFileAsync(new Uri(Constants.CLIENT_DOWNLOAD_URL), Constants.ZIP_PATH);
            }

        }

        private void OnDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            _downloadProgressTracker.SetProgress(e.BytesReceived, e.TotalBytesToReceive);
            updateProgressBar.Value = e.ProgressPercentage;
            updateLabelText.Text = string.Format("Downloading: {0} of {1} @ {2}", StringUtility.FormatBytes(e.BytesReceived),
                StringUtility.FormatBytes(e.TotalBytesToReceive), _downloadProgressTracker.GetBytesPerSecondString());

        }

        private void OnDownloadCompleted(object sender, AsyncCompletedEventArgs e)
        {
            _downloadProgressTracker.Reset();
            updateLabelText.Text = "Download finished - extracting...";

            // Show the extraction progress controls
            extractionProgressBar.Visible = true;
            extractionStatusLabel.Visible = true;

            // Create an instance of the Extract class
            Extract extract = new Extract(this);

            // Attach event handlers to update the extraction progress
            extract.ProgressChanged += ExtractionProgressChanged;
            extract.ExtractionCompleted += ExtractionCompleted;

            // Start the extraction process asynchronously
            extract.Run();
        }

        private void ExtractionProgressChanged(object sender, ExtractProgressEventArgs e)
        {
            // Update the extraction progress bar value
            extractionProgressBar.Value = (int)e.ProgressPercentage;
        }

        private void ExtractionCompleted(object sender, EventArgs e)
        {
            // Hide the extraction progress controls
            extractionProgressBar.Visible = false;
            extractionStatusLabel.Visible = false;

            // Set the launcher ready
            SetLauncherReady();
        }
        public void SetLauncherReady()
        {
            updateLabelText.Text = "";
            if (!File.Exists(Constants.GAME_EXECUTABLE_PATH))
            {
                MessageBox.Show("Couldn't make a connection to the game server. Please try again later or inform the developer if the issue persists.", "Fatal error");
                return;
            }

            currentVersionLabel.Text = "Current Version: " + OnlineVersion;
            Properties.Settings.Default.VersionText = OnlineVersion.ToString();
            Properties.Settings.Default.Save();
            Console.WriteLine("Updated version. Now running on version: " + LocalVersion);
            IsReady = true;

            if (Constants.AUTOMATICALLY_LAUNCH_GAME_AFTER_UPDATING)
                LaunchGame();

            try
            {
                File.Delete(Constants.ZIP_PATH);
            }
            catch
            {
                MessageBox.Show("Couldn't delete the downloaded zip file after extraction.");
            }
        }

        private void FetchPatchNotes()
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(Constants.PATCH_NOTES_URL);

                foreach (XmlNode node in doc.DocumentElement)
                {
                    PatchNoteBlock block = new PatchNoteBlock();
                    for (int i = 0; i < node.ChildNodes.Count; i++)
                    {
                        switch (i)
                        {
                            case 0:
                                block.Title = node.ChildNodes[i].InnerText;
                                break;
                            case 1:
                                block.Text = node.ChildNodes[i].InnerText;
                                break;
                            case 2:
                                block.Link = node.ChildNodes[i].InnerText;
                                break;
                        }
                    }
                    patchNoteBlocks.Add(block);
                }
            }
            catch
            {
                patchContainerPanel.Visible = false;
                if (Constants.SHOW_ERROR_BOX_IF_PATCH_NOTES_DOWNLOAD_FAILS)
                    MessageBox.Show("The launcher was unable to retrieve patch notes from the server!");
            }

            Label[] patchTitleObjects = { patchTitle1, patchTitle2, patchTitle3 };
            Label[] patchTextObjects = { patchText1, patchText2, patchText3 };

            for (int i = 0; i < patchNoteBlocks.Count; i++)
            {
                patchTitleObjects[i].Text = patchNoteBlocks[i].Title;
                patchTextObjects[i].Text = patchNoteBlocks[i].Text;
            }
        }

        private void LaunchGame()
        {
            try
            {
                if (debugCheckbox.Checked)
                {
                    Process.Start(Constants.GAME_EXECUTABLE_PATH, "debug");
                }
                else
                {
                    Process.Start(Constants.GAME_EXECUTABLE_PATH);
                }
                Environment.Exit(0);
            }
            catch
            {
                IsReady = false;
                DownloadFile();
                // MessageBox.Show("Couldn't locate the game executable! Attempting to redownload - please wait.", "Fatal Error");
            }
        }

        private void TogglePlayButton(bool toggle)
        {
            if (File.Exists(Constants.GAME_EXECUTABLE_PATH))
            {
                // Game executable exists, set button to "Play" or "Update"
                switch (toggle)
                {
                    case true:
                        playButton.BackColor = Color.Green;
                        playButton.Text = "Play";
                        break;
                    case false:
                        playButton.BackColor = Color.DeepSkyBlue;
                        playButton.Text = "Update";
                        break;
                }
            }
            else
            {
                // Game executable missing, set button to "Install"
                playButton.BackColor = Color.Orange;
                playButton.Text = "Install";
            }
        }

        // Move the form with LMB
        private void Application_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                NativeWinAPI.ReleaseCapture();
                NativeWinAPI.SendMessage(Handle, NativeWinAPI.WM_NCLBUTTONDOWN, NativeWinAPI.HT_CAPTION, 0);
            }
        }

        private void SetUpButtonEvents()
        {
            Button[] buttons = { navbarButton1, navbarButton2, navbarButton3 };

            for (int i = 0; i < buttons.Length; i++)
            {
                buttons[i].Click += new EventHandler(OnClickButton);
                buttons[i].Text = Constants.NAVBAR_BUTTON_TEXT_ARRAY[i];
            }
        }

        public void OnClickButton(object sender, EventArgs e)
        {
            Button button = (Button)sender;
            switch (button.Name)
            {
                case nameof(navbarButton1):
                    System.Diagnostics.Process.Start(Constants.NAVBAR_BUTTON_1_URL);
                    break;
                case nameof(navbarButton2):
                    System.Diagnostics.Process.Start(Constants.NAVBAR_BUTTON_2_URL);
                    break;
                case nameof(navbarButton3):
                    System.Diagnostics.Process.Start(Constants.NAVBAR_BUTTON_3_URL);
                    break;
                case nameof(patchButton1):
                    Process.Start(patchNoteBlocks[0].Link);
                    break;
                case nameof(patchButton2):
                    Process.Start(patchNoteBlocks[1].Link);
                    break;
                case nameof(patchButton3):
                    Process.Start(patchNoteBlocks[2].Link);
                    break;
            }
        }

        private void OnMouseEnterIcon(object sender, EventArgs e)
        {
            var pictureBox = (PictureBox)sender;
            pictureBox.BackColor = Color.FromArgb(50, 255, 255, 255);
        }

        private void OnMouseLeaveIcon(object sender, EventArgs e)
        {
            var pictureBox = (PictureBox)sender;
            pictureBox.BackColor = Color.FromArgb(0, 255, 255, 255);
        }

        private void minimizePictureBox_Click(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Minimized;
        }

        private void closePictureBox_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void navbarPanel_Paint(object sender, PaintEventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }
    }
}
