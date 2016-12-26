using ExtractWizard.Gateway;
using ExtractWizard.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
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

            ResetView();
        }

        /// <summary>
        /// Resets the View. Populates all fields to their default values and gets ready to extract yet another archive.
        /// </summary>
        public void ResetView()
        {
            _gateway.SetBackupArchivePath("");
            _gateway.SetOutputFolderPath("");
            _gateway.SetPassword("");
            _gateway.SetIgnoreFileWriteErrors(true);
            _gateway.SetDryRun(false);
            _gateway.SetExtractionOptionsState(true);
            _gateway.SetExtractButtonText(_languageResource.GetString("BTN_EXTRACT"));
            _gateway.SetExtractionProgress(0);
            _gateway.SetExtractedFileName("");
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
    }
}
