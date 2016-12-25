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
    /// Controller for the MainForm view
    /// </summary>
    class MainForm
    {
        private IMainFormGateway _gateway;

        private ResourceManager _languageResource;

        public MainForm(IMainFormGateway gateway)
        {
            _gateway = gateway;
            _languageResource = Language.ResourceManager;
        }

        public void IntializeView()
        {
            var version = Assembly.GetCallingAssembly().GetName().Version;

            _gateway.SetWindowTitle($"Akeeba eXtract Wizard {version}");
            _gateway.TranslateInterface(_languageResource);
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
