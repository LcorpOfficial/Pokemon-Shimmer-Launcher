using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UniversalGameLauncher
{
    public class ExtractProgressEventArgs : EventArgs
    {
        public float ProgressPercentage { get; }

        public ExtractProgressEventArgs(float progressPercentage)
        {
            ProgressPercentage = progressPercentage;
        }
    }
    class Extract
    {

        private Application _application;
        public event EventHandler<ExtractProgressEventArgs> ProgressChanged;
        public event EventHandler ExtractionCompleted;
        public Extract(Application application)
        {
            _application = application;
        }

        public void Run()
        {
            BackgroundWorker bgw = new BackgroundWorker
            {
                WorkerReportsProgress = true
            };

            // Create ManualResetEvent for synchronization
            ManualResetEvent deletionCompletedEvent = new ManualResetEvent(false);

            bgw.DoWork += new DoWorkEventHandler(
                delegate (object o, DoWorkEventArgs args)
                {
                    BackgroundWorker bw = o as BackgroundWorker;

                    // Wait for the file to be released
                    bool fileLocked = true;
                    int retries = 0;
                    while (fileLocked && retries < Constants.MAX_EXTRACTION_RETRIES)
                    {
                        try
                        {
                            using (FileStream fs = new FileStream(Constants.ZIP_PATH, FileMode.Open, FileAccess.Read, FileShare.Read))
                            {
                                fileLocked = false;
                            }
                        }
                        catch
                        {
                            // If the file is locked, wait a bit and try again
                            Thread.Sleep(Constants.EXTRACTION_RETRY_INTERVAL);
                            retries++;
                        }
                    }

                    if (fileLocked)
                    {
                        throw new Exception("Unable to extract ZIP file: file is still locked.");
                    }

                    // Delete the directories before extraction
                    DeleteDirectories();

                    // Signal completion of deletion
                    deletionCompletedEvent.Set();

                    FastZip fastZip = new FastZip();
                    fastZip.ExtractZip(Constants.ZIP_PATH, Constants.DESTINATION_PATH, null);
                    ExtractionCompleted?.Invoke(this, EventArgs.Empty);
                    Console.WriteLine("Finished Extracting");
                }
            );

            bgw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(
                delegate (object o, RunWorkerCompletedEventArgs args)
                {
                    _application.SetLauncherReady();
                }
            );

            bgw.RunWorkerAsync();

            // Wait for deletion to complete before proceeding
            deletionCompletedEvent.WaitOne();
        }
        private void FastZip_ExtractProgress(object sender, ICSharpCode.SharpZipLib.Core.ProgressEventArgs e)
        {
            float progressPercentage = e.PercentComplete * 100;

            // Raise the ProgressChanged event
            ProgressChanged?.Invoke(this, new ExtractProgressEventArgs(progressPercentage));
        }
        private void DeleteDirectories()
        {
            string[] directoriesToDelete = new string[]
            {
        "Audio",
        "Data",
        "Fonts",
        "Graphics",
        "PBS",
        "Plugins"
            };

            foreach (string directoryName in directoriesToDelete)
            {
                string directoryPath = Path.Combine(Constants.DESTINATION_PATH, directoryName);
                if (Directory.Exists(directoryPath))
                {
                    Directory.Delete(directoryPath, true);
                }
            }
        }
    }
}