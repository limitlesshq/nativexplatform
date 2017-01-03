//-----------------------------------------------------------------------
// <copyright file="extractCLIProgram.cs" company="Akeeba Ltd">
// Copyright (c) 2006-2016  Nicholas K. Dionysopoulos / Akeeba Ltd
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation the
// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the
// Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Threading;
using System.Threading.Tasks;
using Akeeba.Unarchiver;
using Akeeba.Unarchiver.DataWriter;
using Akeeba.Unarchiver.EventArgs;
using ExtractWizard.Gateway;
using ExtractWizard.Helpers;
using ExtractWizard.Resources;

namespace ExtractWizard.Controller
{
    /// <summary>
    /// Controller for the MainForm view.
    /// 
    /// Technically this is a Presenter since we're following the Passive View pattern, not classic
    /// MVC, but it's easier -even though semanticaly correct- to think about it in terms of MVC.
    /// </summary>
    public class MainForm
    {
        /// <summary>
        /// The cancelation token source object, used to cancel the archive extraction when needed.
        /// </summary>
        private CancellationTokenSource _tokenSource = null;

        /// <summary>
        /// The total size of all parts of the backup archive in bytes.
        /// </summary>
        private ulong _totalArchiveSize = 0;

        /// <summary>
        /// The link to the PayPal donation page
        /// </summary>
        private const string DonationLink = "https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=D9CFZ4H35NFWW";

        /// <summary>
        /// The link to the documentation page
        /// </summary>
        private const string HelpLink = "https://www.akeebabackup.com/documentation/extract-wizard.html";

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
            _gateway.SetExtractButtonText(_languageResource, "BTN_EXTRACT");
            _gateway.SetExtractionProgress(0);
            _gateway.SetExtractedFileName("");
            _gateway.SetTaskbarProgressState(TaskBarProgress.TaskbarStates.NoProgress);
            _gateway.SetTaskbarProgressValue(0);

            if (_tokenSource != null)
            {
                _tokenSource.Dispose();
                _tokenSource = null;
            }
        }

        /// <summary>
        /// Browse for an archive file and update the interface
        /// </summary>
        /// <param name="sender">The button UI control which was clicked</param>
        /// <param name="e">Event arguments</param>
        public void OnBrowseArchiveButtonClick(object sender, EventArgs e)
        {
			string title = _languageResource.GetString("LBL_HEADER_SELECT_ARCHIVE");
            string fileName = _gateway.GetBackupArchivePath();
			string[,] patterns = {
				{_languageResource.GetString("LBL_FILETYPE_JPA"), "*.jpa"},
				{_languageResource.GetString("LBL_FILETYPE_JPS"), "*.jps"},
				{_languageResource.GetString("LBL_FILETYPE_ZIP"), "*.zip"}
			};

			fileName = _gateway.pickFile(title, fileName, patterns, _languageResource.GetString("BTN_OPEN"), _languageResource.GetString("BTN_CANCELDIALOG"));

			// Did the user not select a file?
			if (fileName == "")
			{
				return;
			}

            // Update the archive path
            _gateway.SetBackupArchivePath(fileName);

            // If the extraction path is empty set it to be the same path as the archive file
            if (_gateway.GetOutputFolderPath() != "")
            {
                return;
            }

            _gateway.SetOutputFolderPath(Path.GetDirectoryName(fileName));
        }

        /// <summary>
        /// Browse for an output folder and update the interface
        /// </summary>
        /// <param name="sender">The button UI control which was clicked</param>
        /// <param name="e">Event arguments</param>
        public void OnBrowseOutputFolderButtonClick(object sender, EventArgs e)
        {
            string folderName = _gateway.GetOutputFolderPath();
			string title = _languageResource.GetString("LBL_HEADER_SELECT_FOLDER");
			folderName = _gateway.pickFolder(title, folderName, _languageResource.GetString("BTN_OPEN"), _languageResource.GetString("BTN_CANCELDIALOG"));

			// Did the user not select a folder?
			if (folderName == "")
			{
				return;
			}

            // Update the output directory
            _gateway.SetOutputFolderPath(folderName);
        }

        /// <summary>
        /// Open the donation page
        /// </summary>
        /// <param name="sender">The button UI control which was clicked</param>
        /// <param name="e">Event arguments</param>
        public void OnDonateButtonClick(object sender, EventArgs e)
        {
            Process.Start(DonationLink);
        }

        /// <summary>
        /// Open the help (documentation) page
        /// </summary>
        /// <param name="sender">The button UI control which was clicked</param>
        /// <param name="e">Event arguments</param>
        public void OnHelpButtonClick(object sender, EventArgs e)
        {
            Process.Start(HelpLink);
        }

        /// <summary>
        /// Handle the clicks of the start / stop button
        /// </summary>
        /// <param name="sender">The button UI control which was clicked</param>
        /// <param name="e">Event arguments</param>
        public void OnStartStopButtonClick(object sender, EventArgs e)
        {
            if (_tokenSource == null)
            {
                _gateway.SetExtractButtonText(_languageResource, "BTN_CANCEL");
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

            _tokenSource = new CancellationTokenSource();
            CancellationToken token = _tokenSource.Token;

            try
            {
                using (Unarchiver extractor = Unarchiver.CreateForFile(archiveFile, password))
                {
                    // Wire events
                    _totalArchiveSize = 0;

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
				_gateway.showErrorMessage(_languageResource.GetString("LBL_ERROR_CAPTION"), e.Message);
            }

            _gateway.SetExtractionOptionsState(true);
            ResetProgress();
        }

        /// <summary>
        /// Cancel the archive extraction
        /// </summary>
        private void StopExtraction()
        {
            _tokenSource.Cancel();
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
					_gateway.showErrorMessage(_languageResource.GetString("LBL_ERROR_CAPTION"), e.Progress.LastException.Message);
                    break;

                case ExtractionStatus.Running:
                    // Set the progress bar's percentage
                    if (_totalArchiveSize <= 0)
                    {
                        _gateway.SetExtractionProgress(0);

                        return;
                    }

                    double progress = e.Progress.FilePosition / (float) _totalArchiveSize;
                    double progressPercent = 100 * progress;
                    int percentage = (int) Math.Floor(progressPercent);

                    percentage = Math.Max(0, percentage);
                    percentage = Math.Min(100, percentage);

                    _gateway.SetExtractionProgress(percentage);
                    _gateway.SetTaskbarProgressValue(percentage);

                    break;

                case ExtractionStatus.Finished:
                    // Show OK message
					_gateway.showInfoMessage(_languageResource.GetString("LBL_ERROR_CAPTION"), _languageResource.GetString("LBL_SUCCESS_BODY"));
                    break;

                case ExtractionStatus.Idle:
                    // Show cancelation message
                    _gateway.SetTaskbarProgressState(TaskBarProgress.TaskbarStates.Paused);
					_gateway.showInfoMessage(_languageResource.GetString("LBL_ERROR_CAPTION"), _languageResource.GetString("LBL_CANCEL_BODY"));

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
            _totalArchiveSize = a.ArchiveInformation.ArchiveSize;
        }
    }
}
