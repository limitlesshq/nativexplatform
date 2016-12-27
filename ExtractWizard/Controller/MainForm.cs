using Akeeba.Unarchiver;
using Akeeba.Unarchiver.DataWriter;
using Akeeba.Unarchiver.EventArgs;
using ExtractWizard.Gateway;
using ExtractWizard.Resources;
using ExtractWizard.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ExtractWizard.Controller
{
    /// <summary>
    /// Controller for the MainForm view.
    /// 
    /// Technically this is a Presenter since we're following the Passive View pattern, not classic
    /// MVC, but it's easier -even though semanticaly correct- to think about it in terms of MVC.
    /// </summary>
    class MainForm
    {
        /// <summary>
        /// The cancelation token source object, used to cancel the archive extraction when needed.
        /// </summary>
        private CancellationTokenSource tokenSource = null;

        /// <summary>
        /// The total size of all parts of the backup archive in bytes.
        /// </summary>
        private ulong totalArchiveSize = 0;

        /// <summary>
        /// The link to the PayPal donation page
        /// </summary>
        private const string _donationLink = "https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=D9CFZ4H35NFWW";

        /// <summary>
        /// The link to the documentation page
        /// </summary>
        private const string _helpLink = "https://www.akeebabackup.com/documentation/extract-wizard.html";

        /// <summary>
        /// The Gateway to the main form
        /// </summary>
        private IMainFormGateway _gateway;

        /// <summary>
        /// The language resource we're using for translating the interface
        /// </summary>
        private ResourceManager _languageResource;

        /// <summary>
        /// Public constructor.
        /// </summary>
        /// <param name="gateway">The Gateway to the MainForm view. Must implement the IMainFormGateway interface.</param>
        public MainForm(IMainFormGateway gateway)
        {
            _gateway = gateway;
            _languageResource = Language.ResourceManager;
        }

        /// <summary>
        /// Initializes the view. To be used when the form is first displayed.
        /// </summary>
        public void IntializeView()
        {
            var version = Assembly.GetCallingAssembly().GetName().Version;

            _gateway.SetWindowTitle($"Akeeba eXtract Wizard {version}");
            _gateway.TranslateInterface(_languageResource);

            ResetOptions();
            ResetProgress();
        }

        /// <summary>
        /// Resets the options panel. Populates all fields to their default values.
        /// </summary>
        private void ResetOptions()
        {
            _gateway.SetBackupArchivePath("");
            _gateway.SetOutputFolderPath("");
            _gateway.SetPassword("");
            _gateway.SetIgnoreFileWriteErrors(true);
            _gateway.SetDryRun(false);
            _gateway.SetExtractionOptionsState(true);
        }

        /// <summary>
        /// Resets the progress panel and the cancellation token source.
        /// </summary>
        private void ResetProgress()
        {
            _gateway.SetExtractButtonText(_languageResource.GetString("BTN_EXTRACT"));
            _gateway.SetExtractionProgress(0);
            _gateway.SetExtractedFileName("");
            _gateway.SetTaskbarProgressState(TaskBarProgress.TaskbarStates.NoProgress);
            _gateway.SetTaskbarProgressValue(0);

            if (tokenSource != null)
            {
                tokenSource.Dispose();
                tokenSource = null;
            }
        }

        /// <summary>
        /// Browse for an archive file and update the interface
        /// </summary>
        /// <param name="sender">The button UI control which was clicked</param>
        /// <param name="e">Event arguments</param>
        public void onBrowseArchiveButtonClick(object sender, EventArgs e)
        {
            string fileName = "";

            using (OpenFileDialog fileDialog = new OpenFileDialog())
            {
                // Set up the dialog
                fileDialog.AddExtension = true;
                fileDialog.AutoUpgradeEnabled = true;
                fileDialog.CheckFileExists = true;
                fileDialog.CheckPathExists = true;
                fileDialog.DefaultExt = "jpa";
                fileDialog.DereferenceLinks = true;
                fileDialog.Filter = $"Backup archives (*.jpa)|*.jpa|Encrypted archives (*.jps)|*.jps|ZIP backup archives (*.zip)|*.zip";
                fileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                fileDialog.Multiselect = false;
                fileDialog.SupportMultiDottedExtensions = true;
                fileDialog.Title = "Select a backup archive";

                // Show the dialog
                DialogResult dialogResult = fileDialog.ShowDialog();

                // Did the user cancel the dialog?
                if (dialogResult != DialogResult.OK)
                {
                    return;
                }

                fileName = fileDialog.FileName;

                // Did the user not select a file?
                if (fileName == "")
                {
                    return;
                }

            }

            // Update the archive path
            _gateway.SetBackupArchivePath(fileName);

            // If the extraction path is empty set it to be the same path as the archive file
            if (_gateway.GetOutputFolderPath() != "")
            {
                return;
            }

            _gateway.SetOutputFolderPath(System.IO.Path.GetDirectoryName(fileName));
        }

        /// <summary>
        /// Browse for an output folder and update the interface
        /// </summary>
        /// <param name="sender">The button UI control which was clicked</param>
        /// <param name="e">Event arguments</param>
        public void onBrowseOutputFolderButtonClick(object sender, EventArgs e)
        {
            string folderName = "";

            using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
            {
                // Set up the dialog
                //folderDialog.RootFolder = Environment.SpecialFolder.MyComputer;
                folderDialog.ShowNewFolderButton = true;

                // Show the dialog
                DialogResult dialogResult = folderDialog.ShowDialog();

                // Did the user cancel the dialog?
                if (dialogResult != DialogResult.OK)
                {
                    return;
                }

                folderName = folderDialog.SelectedPath;

                // Did the user not select a folder?
                if (folderName == "")
                {
                    return;
                }

            }

            // Update the output directory
            _gateway.SetOutputFolderPath(folderName);
        }

        /// <summary>
        /// Open the donation page
        /// </summary>
        /// <param name="sender">The button UI control which was clicked</param>
        /// <param name="e">Event arguments</param>
        public void onDonateButtonClick(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(_donationLink);
        }

        /// <summary>
        /// Open the help (documentation) page
        /// </summary>
        /// <param name="sender">The button UI control which was clicked</param>
        /// <param name="e">Event arguments</param>
        public void onHelpButtonClick(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(_helpLink);
        }

        /// <summary>
        /// Handle the clicks of the start / stop button
        /// </summary>
        /// <param name="sender">The button UI control which was clicked</param>
        /// <param name="e">Event arguments</param>
        public void onStartStopButtonClick(object sender, EventArgs e)
        {
            if (tokenSource == null)
            {
                _gateway.SetExtractButtonText(_languageResource.GetString("BTN_CANCEL"));
                _gateway.SetExtractionOptionsState(false);

                _gateway.SetTaskbarProgressState(TaskBarProgress.TaskbarStates.Normal);
                _gateway.SetTaskbarProgressValue(0);

                StartExtractionAsync();

                return;
            }

            StopExtraction();
        }

        /// <summary>
        /// Start the archive extraction asynchronously.
        /// </summary>
        /// <returns></returns>
        private async Task StartExtractionAsync()
        {
            string archiveFile = _gateway.GetBackupArchivePath();
            string outputDirectory = _gateway.GetOutputFolderPath();
            string password = _gateway.GetPassword();
            bool ignoreWriteErrors = _gateway.GetIgnoreFileWriteErrors();
            bool dryRun = _gateway.GetDryRun();

            tokenSource = new CancellationTokenSource();
            CancellationToken token = tokenSource.Token;

            try
            {
                using (Unarchiver extractor = Unarchiver.CreateForFile(archiveFile, password))
                {
                    // Wire events
                    totalArchiveSize = 0;

                    extractor.ArchiveInformationEvent += onArchiveInformationHandler;
                    extractor.ProgressEvent += OnProgressHandler;
                    extractor.EntityEvent += onEntityHandler;

                    Task t = Task.Factory.StartNew(
                        () =>
                        {
                            if (extractor == null)
                            {
                                throw new Exception("Internal state consistency violation: extractor object is null");
                            }

                        // Get the appropriate writer
                        IDataWriter writer = new NullWriter();

                            if (!dryRun)
                            {
                                writer = new DirectFileWriter(outputDirectory);
                            }

                        // Test the extraction
                        extractor.Extract(token, writer);
                        }, token,
                        TaskCreationOptions.None,
                        TaskScheduler.Default
                    );

                    await t;
                }
            }
            catch (Exception e)
            {
                Exception targetException = (e.InnerException == null) ? e : e.InnerException;

                _gateway.SetTaskbarProgressState(TaskBarProgress.TaskbarStates.Error);

                // Show error message
                MessageBox.Show(e.Message, _languageResource.GetString("LBL_ERROR_CAPTION"), MessageBoxButtons.OK);
            }

            _gateway.SetExtractionOptionsState(true);
            ResetProgress();
        }

        /// <summary>
        /// Cancel the archive extraction
        /// </summary>
        private void StopExtraction()
        {
            tokenSource.Cancel();
        }

        /// <summary>
        /// Handle the progress event of the unarchiver
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnProgressHandler(object sender, ProgressEventArgs e)
        {
            switch (e.Progress.Status)
            {
                case ExtractionStatus.Error:
                    // Set progress status to error
                    _gateway.SetTaskbarProgressState(TaskBarProgress.TaskbarStates.Error);

                    // Show error message
                    MessageBox.Show(e.Progress.LastException.Message, _languageResource.GetString("LBL_ERROR_CAPTION"), MessageBoxButtons.OK);
                    break;

                case ExtractionStatus.Running:
                    // Set the progress bar's percentage
                    if (totalArchiveSize <= 0)
                    {
                        _gateway.SetExtractionProgress(0);

                        return;
                    }

                    double progress = e.Progress.FilePosition / (float) totalArchiveSize;
                    double progressPercent = 100 * progress;
                    int percentage = (int) Math.Floor(progressPercent);

                    percentage = Math.Max(0, percentage);
                    percentage = Math.Min(100, percentage);

                    _gateway.SetExtractionProgress(percentage);
                    _gateway.SetTaskbarProgressValue(percentage);

                    break;

                case ExtractionStatus.Finished:
                    // Show OK message
                    MessageBox.Show(_languageResource.GetString("LBL_SUCCESS_BODY"), _languageResource.GetString("LBL_SUCCESS_CAPTION"), MessageBoxButtons.OK);
                    break;

                case ExtractionStatus.Idle:
                    // Show cancelation message
                    _gateway.SetTaskbarProgressState(TaskBarProgress.TaskbarStates.Paused);
                    MessageBox.Show(_languageResource.GetString("LBL_CANCEL_BODY"), _languageResource.GetString("LBL_CANCEL_CAPTION"), MessageBoxButtons.OK);

                    break;
            }
        }

        /// <summary>
        /// Handle the unarchiver's entity event. Used to update the interface with ther name of
        /// the file being currently extracted.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="a"></param>
        private void onEntityHandler(object sender, EntityEventArgs a)
        {
            _gateway.SetExtractedFileName(a.Information.StoredName);
        }

        /// <summary>
        /// Handle the unarchiver's archive information event. We need it to get the total size of
        /// the archive in bytes which we will then use to get the percentage of the file
        /// extracted for the progress bar display.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="a"></param>
        private void onArchiveInformationHandler(object sender, ArchiveInformationEventArgs a)
        {
            totalArchiveSize = a.ArchiveInformation.ArchiveSize;
        }
    }
}
