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
    class Extract
    {

        private Application _application;

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

            bgw.DoWork += new DoWorkEventHandler(
                delegate (object o, DoWorkEventArgs args) {
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

                    FastZip fastZip = new FastZip();
                    fastZip.ExtractZip(Constants.ZIP_PATH, Constants.DESTINATION_PATH, null);
                    Console.WriteLine("Finished Extracting");
                }
            );

            bgw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(
                delegate (object o, RunWorkerCompletedEventArgs args) {
                    _application.SetLauncherReady();
                }
            );

            bgw.RunWorkerAsync();
        }

    }
}
