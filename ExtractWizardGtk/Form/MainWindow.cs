using System;
using System.Resources;
using ExtractWizard.Gateway;
using ExtractWizard.Helpers;
using Gtk;

public partial class MainWindow : Gtk.Window, IMainFormGateway
{
	private ExtractWizard.Controller.MainForm _controller;

	public MainWindow() : base(Gtk.WindowType.Toplevel)
	{
		Build();
		// Create the Controller
		_controller = new ExtractWizard.Controller.MainForm(this);

		_controller.IntializeView();
	}

	/// <summary>
	/// Sets the title of the UI window
	/// </summary>
	/// <param name="title"></param>
	public void SetWindowTitle(string title)
	{
        Gtk.Application.Invoke(delegate
        {
            Title = title;
        });
	}

	/// <summary>
	/// Handles the translation of all labels, buttons etc given a ResourceManager holding all the language strings
	/// </summary>
	public void TranslateInterface(ResourceManager text)
	{
		// Groups
		lblGroupOptions.Text = _lang(text, "GROUP_OPTIONS");
		lblGroupProgress.Text = _lang(text, "GROUP_PROGRESS");

		// Labels
		lblArchive.Text = _lang(text, "LBL_BACKUP_ARCHIVE");
		lblOutputFolder.Text = _lang(text, "LBL_EXTRACT_TO");
		lblPassword.Text = _lang(text, "LBL_PASSWORD");

		// Checkboxes
		chkIgnoreErrors.Label = _lang(text, "CHK_IGNORE_ERRORS");
		chkDryRun.Label = _lang(text, "CHK_DRY_RUN");

		// Buttons
		btnArchive.Label = _lang(text, "BTN_BROWSE_ARCHIVE");
		btnFolder.Label = _lang(text, "BTN_BROWSE_FOLDER");
		btnHelp.Label = _lang(text, "BTN_HELP");
	}

	/// <summary>
	/// Sets the text of the Backup Archive UI element
	/// </summary>
	/// <param name="backupArchivePath"></param>
	public void SetBackupArchivePath(string backupArchivePath)
	{
        Gtk.Application.Invoke(delegate
        {
            editArchive.Text = backupArchivePath;
        });
	}

	/// <summary>
	/// Gets the text of the Backup Archive UI element
	/// </summary>
	/// <returns></returns>
	public string GetBackupArchivePath()
	{
		return editArchive.Text;
	}

	/// <summary>
	/// Sets the text for the Output Directory UI element
	/// </summary>
	/// <param name="outputFolderPath"></param>
	public void SetOutputFolderPath(string outputFolderPath)
	{
        Gtk.Application.Invoke(delegate
        {
            editOutputFolder.Text = outputFolderPath;
        });
	}

	/// <summary>
	/// Gets the text for the Output Directory UI element
	/// </summary>
	/// <returns></returns>
	public string GetOutputFolderPath()
	{
		return editOutputFolder.Text;
	}

	/// <summary>
	/// Gets the text of the JPS Password UI element
	/// </summary>
	/// <returns></returns>
	public void SetPassword(string password)
	{
        Gtk.Application.Invoke(delegate
        {
            editPassword.Text = password;
        });
	}

	/// <summary>
	/// Gets the text of the JPS Password UI element
	/// </summary>
	/// <returns></returns>
	public string GetPassword()
	{
		return editPassword.Text;
	}

	/// <summary>
	/// Gets the value of the Ignore File Write Errors UI element
	/// </summary>
	/// <returns></returns>
	public bool GetIgnoreFileWriteErrors()
	{
		return chkIgnoreErrors.Active;
	}

	/// <summary>
	/// Sets the value of the Ignore File Write Errors UI element
	/// </summary>
	/// <returns></returns>
	public void SetIgnoreFileWriteErrors(bool isChecked)
	{
        Gtk.Application.Invoke(delegate
        {
            chkIgnoreErrors.Active = isChecked;
        });
	}

	/// <summary>
	/// Gets the value of the Dry Run UI element
	/// </summary>
	/// <returns></returns>
	public bool GetDryRun()
	{
		return chkDryRun.Active;
	}

	/// <summary>
	/// Sets the value of the Dry Run UI element
	/// </summary>
	/// <returns></returns>
	public void SetDryRun(bool isChecked)
	{
        Gtk.Application.Invoke(delegate
        {
            chkDryRun.Active = isChecked;
        });
	}

	/// <summary>
	/// Sets the state (enabled / disabled) for all the user-interactive extraction option controls
	/// </summary>
	/// <param name="enabled"></param>
	public void SetExtractionOptionsState(bool enabled)
	{
        Gtk.Application.Invoke(delegate
        {
            // Set state of edit box labels
            lblArchive.Sensitive = enabled;
            lblOutputFolder.Sensitive = enabled;
            lblPassword.Sensitive = enabled;

            // Set state of edit boxes
            editArchive.Sensitive = enabled;
            editOutputFolder.Sensitive = enabled;
            editPassword.Sensitive = enabled;

            // Set state of check boxes
            chkDryRun.Sensitive = enabled;
            chkIgnoreErrors.Sensitive = enabled;

            // Set state of buttons
            btnArchive.Sensitive = enabled;
            btnFolder.Sensitive = enabled;
            btnHelp.Sensitive = enabled;
        });
	}

	/// <summary>
	/// Set the label of the Extract UI button
	/// </summary>
	/// <param name="text">Resource manager handling the translations</param>
	/// <param name="label">The label's translation key</param>
	public void SetExtractButtonText(ResourceManager text, string label)
	{
        Gtk.Application.Invoke(delegate
        {
            btnStartStop.Label = _lang(text, label);
        });
	}

	/// <summary>
	/// Set the percentage of the extraction which is already complete
	/// </summary>
	/// <param name="percent"></param>
	public void SetExtractionProgress(int percent)
	{
        Gtk.Application.Invoke(delegate
        {
            // Squash percentage between 0 - 100
            percent = Math.Max(0, percent);
            percent = Math.Min(100, percent);

            // Set the progress bar value
            progressbarExtraction.Fraction = (double)percent / 100.0;
        });
	}

	/// <summary>
	/// Set the label text for the extracted file name
	/// </summary>
	/// <param name="fileName"></param>
	public void SetExtractedFileName(string fileName)
	{
        Gtk.Application.Invoke(delegate
        {
            lblExtractedFile.Text = fileName;
        });
        
	}

	/// <summary>
	/// Set the taskbar progress bar's state. Has an effect only on Windows 7+.
	/// </summary>
	/// <param name="state"></param>
	public void SetTaskbarProgressState(TaskBarProgress.TaskbarStates state)
	{
		// Intentionally left blank
	}

	/// <summary>
	/// Set the taskbar progress bar's value (whole percentage points, 0-100). Has an effect only on Windows 7+.
	/// </summary>
	/// <param name="value"></param>
	public void SetTaskbarProgressValue(int value)
	{
		// Intentionally left blank
	}

	/// <summary>
	/// Shows an error message dialog in a GUI framework appropriate way.
	/// </summary>
	/// <param name="title">The title of the message dalog.</param>
	/// <param name="message">The error message to present to the user.</param>
	public void showErrorMessage(string title, string message)
	{
        Gtk.Application.Invoke(delegate
        {
            MessageDialog dialog = new MessageDialog(this, 0, MessageType.Error, ButtonsType.Ok, message);
            dialog.Title = title;
            dialog.Run();
            dialog.Destroy();            
        });
	}

	/// <summary>
	/// Shows an information message dialog in a GUI framework appropriate way.
	/// </summary>
	/// <param name="title">The title of the message dalog.</param>
	/// <param name="message">The message to present to the user.</param>
	public void showInfoMessage(string title, string message)
	{
        Gtk.Application.Invoke(delegate
        {
            MessageDialog dialog = new MessageDialog(this, 0, MessageType.Info, ButtonsType.Ok, message);
            dialog.Title = title;
            dialog.Run();
            dialog.Destroy();            
        });
	}

	/// <summary>
	/// Picks an archive file for opening
	/// </summary>
	/// <returns>The path to the file.</returns>
	/// <param name="title">The title of the dialog.</param>
	/// <param name="fileName">The pre-selected file name.</param>
	/// <param name="patterns">File filter patters as an array of {patternName, pattern}.</param>
	/// <param name="OKLabel">The label for the OK button (where supported)</param>
	/// <param name="CancelLabel">The label for the Cancel button (where supported)</param>
	public string pickFile(string title, string fileName, string[,] patterns, string OKLabel, string CancelLabel)
	{
		FileChooserDialog dialog = new FileChooserDialog(title, this, FileChooserAction.Open);

		if (fileName != "")
		{
			dialog.CurrentName = fileName;
			dialog.SetCurrentFolder(fileName);
		}

		dialog.AddButton(CancelLabel, Gtk.ResponseType.Cancel);
		dialog.AddButton(OKLabel, Gtk.ResponseType.Accept);

		for (int i = 0; i < patterns.Length / 2; i++)
		{
			FileFilter filter = new FileFilter();
			filter.Name = patterns[i, 0];
			filter.AddPattern(patterns[i, 1]);
			dialog.AddFilter(filter);
		}

		ResponseType ret = (ResponseType)dialog.Run();
		string newFileName = dialog.Filename;

		dialog.Destroy();

		if (ret != ResponseType.Accept)
		{
			return "";
		}

		return newFileName;
	}

	/// <summary>
	/// Picks a folder for opening
	/// </summary>
	/// <returns>The path to the filder.</returns>
	/// <param name="title">The title of the dialog.</param>
	/// <param name="folderName">The pre-selected folder name.</param>
	/// <param name="OKLabel">The label for the OK button (where supported)</param>
	/// <param name="CancelLabel">The label for the Cancel button (where supported)</param>
	public string pickFolder(string title, string folderName, string OKLabel, string CancelLabel)
	{
		FileChooserDialog dialog = new FileChooserDialog(title, this, FileChooserAction.SelectFolder);

		if (folderName != "")
		{
			dialog.SetCurrentFolder(folderName);
		}

		dialog.AddButton(CancelLabel, Gtk.ResponseType.Cancel);
		dialog.AddButton(OKLabel, Gtk.ResponseType.Accept);

		ResponseType ret = (ResponseType)dialog.Run();
		string newFolderName = dialog.Filename;

		dialog.Destroy();

		if (ret != ResponseType.Accept)
		{
			return "";
		}

		return newFolderName;
	}

	/// <summary>
	/// Gets the language string for the specified tag from the resource text and removes Windows-specific
	/// accelerators.
	/// </summary>
	/// <returns>The translated string.</returns>
	/// <param name="text">Language resource.</param>
	/// <param name="tag">The language tag to use.</param>
	private string _lang(ResourceManager text, string tag)
	{
		return text.GetString(tag).Replace("&", "");
	}

	protected void OnDeleteEvent(object sender, DeleteEventArgs a)
	{
		Application.Quit();
		a.RetVal = true;
	}

	protected void OnBtnArchiveClicked(object sender, EventArgs e)
	{
		_controller.OnBrowseArchiveButtonClick(sender, e);
	}

	protected void OnBtnFolderClicked(object sender, EventArgs e)
	{
		_controller.OnBrowseOutputFolderButtonClick(sender, e);
	}

	protected void OnBtnDonateButtonPressEvent(object o, ButtonPressEventArgs args)
	{
		_controller.OnDonateButtonClick(o, args);
	}

	protected void OnBtnHelpClicked(object sender, EventArgs e)
	{
		_controller.OnHelpButtonClick(sender, e);
	}

	protected void OnBtnStartStopClicked(object sender, EventArgs e)
	{
		_controller.OnStartStopButtonClick(sender, e);
	}
}
