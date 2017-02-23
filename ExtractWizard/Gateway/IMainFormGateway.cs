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

using System.Resources;
using ExtractWizard.Helpers;

namespace ExtractWizard.Gateway
{
    /// <summary>
    /// Interface for the MainForm gateway.
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
    public interface IMainFormGateway
    {
        /// <summary>
        /// Handles the translation of all labels, buttons etc given a ResourceManager holding all the language strings
        /// </summary>
        void TranslateInterface(ResourceManager text);

        /// <summary>
        /// Sets the title of the UI window
        /// </summary>
        /// <param name="title"></param>
        void SetWindowTitle(string title);

        /// <summary>
        /// Sets the text of the Backup Archive UI element
        /// </summary>
        /// <param name="backupArchivePath"></param>
        void SetBackupArchivePath(string backupArchivePath);

        /// <summary>
        /// Gets the text of the Backup Archive UI element
        /// </summary>
        /// <returns></returns>
        string GetBackupArchivePath();

        /// <summary>
        /// Sets the text for the Output Directory UI element
        /// </summary>
        /// <param name="outputFolderPath"></param>
        void SetOutputFolderPath(string outputFolderPath);

        /// <summary>
        /// Gets the text for the Output Directory UI element
        /// </summary>
        /// <returns></returns>
        string GetOutputFolderPath();

        /// <summary>
        /// Gets the text of the JPS Password UI element
        /// </summary>
        /// <returns></returns>
        string GetPassword();

        /// <summary>
        /// Gets the text of the JPS Password UI element
        /// </summary>
        /// <returns></returns>
        void SetPassword(string password);

        /// <summary>
        /// Gets the value of the Ignore File Write Errors UI element
        /// </summary>
        /// <returns></returns>
        bool GetIgnoreFileWriteErrors();

        /// <summary>
        /// Sets the value of the Ignore File Write Errors UI element
        /// </summary>
        /// <returns></returns>
        void SetIgnoreFileWriteErrors(bool isChecked);

        /// <summary>
        /// Gets the value of the Dry Run UI element
        /// </summary>
        /// <returns></returns>
        bool GetDryRun();

        /// <summary>
        /// Sets the value of the Dry Run UI element
        /// </summary>
        /// <returns></returns>
        void SetDryRun(bool isChecked);

        /// <summary>
        /// Sets the state (enabled / disabled) for all the user-interactive extraction option controls
        /// </summary>
        /// <param name="enabled"></param>
        void SetExtractionOptionsState(bool enabled);

        /// <summary>
        /// Set the label of the Extract UI button
        /// </summary>
		/// <param name="text">Resource manager handling the translations</param>
        /// <param name="label">The label's translation key</param>
        void SetExtractButtonText(ResourceManager text, string label);

        /// <summary>
        /// Set the percentage of the extraction which is already complete
        /// </summary>
        /// <param name="percent"></param>
        void SetExtractionProgress(int percent);

        /// <summary>
        /// Set the label text for the extracted file name
        /// </summary>
        /// <param name="fileName"></param>
        void SetExtractedFileName(string fileName);

        /// <summary>
        /// Set the taskbar progress bar's state. Has an effect only on Windows 7+.
        /// </summary>
        /// <param name="state"></param>
        void SetTaskbarProgressState(TaskBarProgress.TaskbarStates state);

        /// <summary>
        /// Set the taskbar progress bar's value (whole percentage points, 0-100). Has an effect only on Windows 7+.
        /// </summary>
        /// <param name="state"></param>
        void SetTaskbarProgressValue(int value);

		/// <summary>
		/// Shows an error message dialog in a GUI framework appropriate way.
		/// </summary>
		/// <param name="title">The title of the message dalog.</param>
		/// <param name="message">The error message to present to the user.</param>
		void showErrorMessage(string title, string message);

		/// <summary>
		/// Shows an information message dialog in a GUI framework appropriate way.
		/// </summary>
		/// <param name="title">The title of the message dalog.</param>
		/// <param name="message">The message to present to the user.</param>
		void showInfoMessage(string title, string message);

		/// <summary>
		/// Picks an archive file for opening
		/// </summary>
		/// <returns>The path to the file.</returns>
		/// <param name="title">The title of the dialog.</param>
		/// <param name="fileName">The pre-selected file name.</param>
		/// <param name="patterns">File filter patters as an array of {patternName, pattern}.</param>
		/// <param name="OKLabel">The label for the OK button (where supported)</param>
		/// <param name="CancelLabel">The label for the Cancel button (where supported)</param>
		string pickFile(string title, string fileName, string[,] patterns, string OKLabel, string CancelLabel);

		/// <summary>
		/// Picks a folder for opening
		/// </summary>
		/// <returns>The path to the filder.</returns>
		/// <param name="title">The title of the dialog.</param>
		/// <param name="folderName">The pre-selected folder name.</param>
		/// <param name="OKLabel">The label for the OK button (where supported)</param>
		/// <param name="CancelLabel">The label for the Cancel button (where supported)</param>
		string pickFolder(string title, string folderName, string OKLabel, string CancelLabel);
    }
}
