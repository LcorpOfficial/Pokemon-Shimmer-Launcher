using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniversalGameLauncher {
    class Constants : Application {

        /// <summary>
        /// Core game info
        /// </summary>
        public static readonly string GAME_TITLE = "Pokémon Shimmer";
        public static readonly string LAUNCHER_NAME = "Pokémon Shimmer Launcher";

        /// <summary>
        /// Paths & urls
        /// </summary>
        public static readonly string DESTINATION_PATH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), GAME_TITLE);
        public static readonly string ZIP_PATH = Path.Combine(DESTINATION_PATH, GAME_TITLE + ".zip");
        public static readonly string GAME_EXECUTABLE_PATH = Path.Combine(DESTINATION_PATH, "Game.exe");

        public static readonly string VERSION_URL = "https://cdn.lcorpcity.com/version.txt";
        public static readonly string APPLICATION_ICON_URL = "https://media.lcorpcity.com/img/web/adminball.ico"; // Needs to be .ico
        public static readonly string LOGO_URL = "https://media.lcorpcity.com/img/web/shimmerlogo.png"; // Ideally around 283x75
        public static readonly string BACKGROUND_URL = "https://media.lcorpcity.com/img/web/shimmersplash.png";
        public static readonly string PATCH_NOTES_URL = "https://cdn.lcorpcity.com/shimmerpatch.xml";
        public static readonly string CLIENT_DOWNLOAD_URL = "https://cdn.lcorpcity.com/shimmerupdate.zip";

        /// <summary>
        /// Navigation bar buttons
        /// </summary>
        public static readonly string NAVBAR_BUTTON_1_TEXT = "Website";
        public static readonly string NAVBAR_BUTTON_1_URL = "https://lcorpcity.com/";
        public static readonly string NAVBAR_BUTTON_2_TEXT = "News";
        public static readonly string NAVBAR_BUTTON_2_URL = "https://lcorpcity.com/";
        public static readonly string NAVBAR_BUTTON_3_TEXT = "Discord";
        public static readonly string NAVBAR_BUTTON_3_URL = "https://discord.gg/2nFA9CU";
        public static readonly string NAVBAR_BUTTON_4_TEXT = "Support";
        public static readonly string NAVBAR_BUTTON_4_URL = "https://lcorpcity.com/";
        public static readonly string NAVBAR_BUTTON_5_TEXT = "Discord";
        public static readonly string NAVBAR_BUTTON_5_URL = "https://discord.gg/2nFA9CU";

        // Modify this array if you're adding or removing a button
        public static readonly string[] NAVBAR_BUTTON_TEXT_ARRAY = {NAVBAR_BUTTON_1_TEXT, NAVBAR_BUTTON_2_TEXT, NAVBAR_BUTTON_3_TEXT,
                                                                    NAVBAR_BUTTON_4_TEXT, NAVBAR_BUTTON_5_TEXT };

        /// <summary>
        /// Settings
        /// </summary>
        public static bool SHOW_VERSION_TEXT = true;
        public static bool AUTOMATICALLY_BEGIN_UPDATING = false;
        public static bool AUTOMATICALLY_LAUNCH_GAME_AFTER_UPDATING = false;
        public static bool SHOW_ERROR_BOX_IF_PATCH_NOTES_DOWNLOAD_FAILS = true;

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Constants));
            this.SuspendLayout();
            // 
            // Constants
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("$this.BackgroundImage")));
            this.ClientSize = new System.Drawing.Size(1028, 643);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Location = new System.Drawing.Point(0, 0);
            this.Name = "Constants";
            this.Text = "Pokémon Shimmer Launcher";
            this.ResumeLayout(false);
            this.PerformLayout();

        }
    }
}
