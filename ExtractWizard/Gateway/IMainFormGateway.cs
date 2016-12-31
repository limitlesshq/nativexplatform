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
        /// <param name="BackupArchivePath"></param>
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
        /// <param name="label"></param>
        void SetExtractButtonText(string label);

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
    }
}
