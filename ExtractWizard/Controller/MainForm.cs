using ExtractWizard.Gateway;
using ExtractWizard.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Threading.Tasks;

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
    }
}
