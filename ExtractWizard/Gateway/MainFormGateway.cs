using ExtractWizard.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ExtractWizard.Gateway
{
    /// <summary>
    /// Gateway to the MainForm View (form).
    /// 
    /// The gateway is used to abstract the View (form) interaction for the Controller. The Controller is only ever given
    /// a reference to the gateway. This allows us to create a test double which doesn't require implementing the real UI
    /// form. It even allows us to switch to a different UI library or form layout without affecting the way the Controller
    /// works.
    /// 
    /// See:
    /// http://martinfowler.com/eaaCatalog/gateway.html
    /// http://martinfowler.com/eaaDev/PassiveScreen.html
    /// </summary>
    class MainFormGateway : IMainFormGateway
    {
        private MainForm myForm;

        /// <summary>
        /// Public constructor. This specialised gateway expects a MainForm object as input.
        /// </summary>
        /// <param name="myForm"></param>
        public MainFormGateway(MainForm myForm)
        {
            this.myForm = myForm;
        }

        /// <summary>
        /// Sets the title of the UI window
        /// </summary>
        /// <param name="title"></param>
        public void SetWindowTitle(string title)
        {
            myForm.Text = title;
        }

        /// <summary>
        /// Handles the translation of all labels, buttons etc given a ResourceManager holding all the language strings
        /// </summary>
        public void TranslateInterface(ResourceManager text)
        {
            // Groups
            myForm.groupOptions.Text = text.GetString((string)myForm.groupOptions.Tag);
            myForm.groupProgress.Text = text.GetString((string)myForm.groupProgress.Tag);

            // Labels
            myForm.lblBackupArchive.Text = text.GetString((string) myForm.lblBackupArchive.Tag);
            myForm.lblExtractToFolder.Text = text.GetString((string)myForm.lblExtractToFolder.Tag);
            myForm.lblPassword.Text = text.GetString((string)myForm.lblPassword.Tag);

            // Checkboxes
            myForm.chkDryRun.Text = text.GetString((string)myForm.chkDryRun.Tag);
            myForm.chkIgnoreErrors.Text = text.GetString((string)myForm.chkIgnoreErrors.Tag);

            // Buttons
            myForm.btnBrowseArchive.Text = text.GetString((string)myForm.btnBrowseArchive.Tag);
            myForm.btnExtractToFolder.Text = text.GetString((string)myForm.btnExtractToFolder.Tag);
            myForm.btnHelp.Text = text.GetString((string)myForm.btnHelp.Tag);
        }

        /// <summary>
        /// Sets the text of the Backup Archive UI element
        /// </summary>
        /// <param name="BackupArchivePath"></param>
        public void SetBackupArchivePath(string backupArchivePath)
        {
            ThreadHelper.SetText(myForm, myForm.editBackupArchive, backupArchivePath);
        }

        /// <summary>
        /// Gets the text of the Backup Archive UI element
        /// </summary>
        /// <returns></returns>
        public string GetBackupArchivePath()
        {
            return myForm.editBackupArchive.Text;
        }

        /// <summary>
        /// Sets the text for the Output Directory UI element
        /// </summary>
        /// <param name="outputFolderPath"></param>
        public void SetOutputFolderPath(string outputFolderPath)
        {
            ThreadHelper.SetText(myForm, myForm.editExtractToFolder, outputFolderPath);
        }

        /// <summary>
        /// Gets the text for the Output Directory UI element
        /// </summary>
        /// <returns></returns>
        public string GetOutputFolderPath()
        {
            return myForm.editExtractToFolder.Text;
        }

        /// <summary>
        /// Gets the text of the JPS Password UI element
        /// </summary>
        /// <returns></returns>
        public void SetPassword(string password)
        {
            ThreadHelper.SetText(myForm, myForm.editPassword, password);
        }

        /// <summary>
        /// Gets the text of the JPS Password UI element
        /// </summary>
        /// <returns></returns>
        public string GetPassword()
        {
            return myForm.editPassword.Text;
        }

        /// <summary>
        /// Gets the value of the Ignore File Write Errors UI element
        /// </summary>
        /// <returns></returns>
        public bool GetIgnoreFileWriteErrors()
        {
            return myForm.chkIgnoreErrors.Checked;
        }

        /// <summary>
        /// Sets the value of the Ignore File Write Errors UI element
        /// </summary>
        /// <returns></returns>
        public void SetIgnoreFileWriteErrors(bool isChecked)
        {
            ThreadHelper.SetCheckboxEnabled(myForm, myForm.chkIgnoreErrors, isChecked);
        }

        /// <summary>
        /// Gets the value of the Dry Run UI element
        /// </summary>
        /// <returns></returns>
        public bool GetDryRun()
        {
            return myForm.chkDryRun.Checked;
        }

        /// <summary>
        /// Sets the value of the Dry Run UI element
        /// </summary>
        /// <returns></returns>
        public void SetDryRun(bool isChecked)
        {
            ThreadHelper.SetCheckboxEnabled(myForm, myForm.chkDryRun, isChecked);
        }

        /// <summary>
        /// Sets the state (enabled / disabled) for all the user-interactive extraction option controls
        /// </summary>
        /// <param name="enabled"></param>
        public void SetExtractionOptionsState(bool enabled)
        {
            // Set state of edit box labels
            ThreadHelper.SetEnabled(myForm, myForm.lblBackupArchive, enabled);
            ThreadHelper.SetEnabled(myForm, myForm.lblExtractToFolder, enabled);
            ThreadHelper.SetEnabled(myForm, myForm.lblPassword, enabled);

            // Set state of edit boxes
            ThreadHelper.SetEnabled(myForm, myForm.editBackupArchive, enabled);
            ThreadHelper.SetEnabled(myForm, myForm.editExtractToFolder, enabled);
            ThreadHelper.SetEnabled(myForm, myForm.editPassword, enabled);

            // Set state of check boxes
            ThreadHelper.SetEnabled(myForm, myForm.chkDryRun, enabled);
            ThreadHelper.SetEnabled(myForm, myForm.chkIgnoreErrors, enabled);

            // Set state of buttons
            ThreadHelper.SetEnabled(myForm, myForm.btnBrowseArchive, enabled);
            ThreadHelper.SetEnabled(myForm, myForm.btnExtractToFolder, enabled);
            ThreadHelper.SetEnabled(myForm, myForm.btnHelp, enabled);
        }

        /// <summary>
        /// Set the label of the Extract UI button
        /// </summary>
        /// <param name="label"></param>
        public void SetExtractButtonText(string label)
        {
            ThreadHelper.SetText(myForm, myForm.btnExtract, label);
        }

        /// <summary>
        /// Set the percentage of the extraction which is already complete
        /// </summary>
        /// <param name="percent"></param>
        public void SetExtractionProgress(int percent)
        {
            // Squash percentage between 0 - 100
            percent = Math.Max(0, percent);
            percent = Math.Min(100, percent);

            // Set the progress bar value
            ThreadHelper.SetProgressValue(myForm, myForm.progressBarExtract, percent);
        }

        /// <summary>
        /// Set the label text for the extracted file name
        /// </summary>
        /// <param name="fileName"></param>
        public void SetExtractedFileName(string fileName)
        {
            ThreadHelper.SetText(myForm, myForm.lblExtractedFile, fileName);
        }
    }
}
