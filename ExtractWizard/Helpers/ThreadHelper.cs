using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ExtractWizard.Helpers
{
    /// <summary>
    /// A helper class to deal with updating form components' values across threads. This class
    /// is used by the Gateway for thread-safe property updates.
    /// 
    /// Our archive extraction runs asynchronously. This means that the main application thread
    /// (which also handles the UI interaction) spawns a new thread to run the extraction process.
    /// In turn, that thread raises events which are handled within that thread. When we try
    /// updating UI components from that new thread we get an exception since that cross-thread
    /// opeartion is, of course, not permitted. If it were the components could easily get in a
    /// corrupt state or worse when two or more threads tried to change their properties. This can
    /// be overcome by using delegates (callbacks) with the form's Invoke() method. The Invoke()
    /// method will run the delegate (callback) in the form's main thread. Therefore the update
    /// will now run on the correct thread and we get no more exceptions.
    /// 
    /// References:
    /// https://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k(System.Windows.Forms.Control.Invoke);k(TargetFrameworkMoniker-.NETFramework,Version%3Dv4.5.2);k(DevLang-csharp)&rd=true
    /// https://msdn.microsoft.com/en-us/library/ms173171.aspx
    /// http://stackoverflow.com/questions/10775367/cross-thread-operation-not-valid-control-textbox1-accessed-from-a-thread-othe
    /// </summary>
    public static class ThreadHelper
    {
        /// <summary>
        /// Delegate for SetText
        /// </summary>
        /// <param name="form"></param>
        /// <param name="control"></param>
        /// <param name="text"></param>
        delegate void SetTextCallback(Form form, Control control, string text);

        /// <summary>
        /// Set text property of various controls
        /// </summary>
        /// <param name="form">The calling form</param>
        /// <param name="control"></param>
        /// <param name="text"></param>
        public static void SetText(Form form, Control control, string text)
        {
            // InvokeRequired required compares the thread ID of the 
            // calling thread to the thread ID of the creating thread. 
            // If these threads are different, it returns true. 
            if (control.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetText);
                form.Invoke(d, new object[] { form, control, text });

                return;
            }

            control.Text = text;
        }

        /// <summary>
        /// Delegate for SetProgressValue
        /// </summary>
        /// <param name="form"></param>
        /// <param name="control"></param>
        /// <param name="text"></param>
        delegate void SetProgressValueCallback(Form form, ProgressBar control, int value);

        /// <summary>
        /// Set value property of progress bars
        /// </summary>
        /// <param name="form">The calling form</param>
        /// <param name="control"></param>
        /// <param name="value"></param>
        public static void SetProgressValue(Form form, ProgressBar control, int value)
        {
            // InvokeRequired required compares the thread ID of the 
            // calling thread to the thread ID of the creating thread. 
            // If these threads are different, it returns true. 
            if (control.InvokeRequired)
            {
                SetProgressValueCallback d = new SetProgressValueCallback(SetProgressValue);
                form.Invoke(d, new object[] { form, control, value });

                return;
            }

            control.Value = value;
        }

        /// <summary>
        /// Delegate for SetCheckboxEnabled
        /// </summary>
        /// <param name="form"></param>
        /// <param name="control"></param>
        /// <param name="text"></param>
        delegate void SetCheckboxEnabledCallback(Form form, CheckBox control, bool value);

        /// <summary>
        /// Set the Checked property of check boxes
        /// </summary>
        /// <param name="form">The calling form</param>
        /// <param name="control"></param>
        /// <param name="value"></param>
        public static void SetCheckboxEnabled(Form form, CheckBox control, bool value)
        {
            // InvokeRequired required compares the thread ID of the 
            // calling thread to the thread ID of the creating thread. 
            // If these threads are different, it returns true. 
            if (control.InvokeRequired)
            {
                SetCheckboxEnabledCallback d = new SetCheckboxEnabledCallback(SetCheckboxEnabled);
                form.Invoke(d, new object[] { form, control, value });

                return;
            }

            control.Checked = value;
        }

        /// <summary>
        /// Delegate for SetEnabled
        /// </summary>
        /// <param name="form"></param>
        /// <param name="control"></param>
        /// <param name="text"></param>
        delegate void SetEnabledCallback(Form form, Control control, bool value);

        /// <summary>
        /// Set the Enabled property of check boxes
        /// </summary>
        /// <param name="form">The calling form</param>
        /// <param name="control"></param>
        /// <param name="value"></param>
        public static void SetEnabled(Form form, Control control, bool value)
        {
            if (control.InvokeRequired)
            {
                SetCheckboxEnabledCallback d = new SetCheckboxEnabledCallback(SetEnabled);
                form.Invoke(d, new object[] { form, control, value });

                return;
            }

            control.Enabled = value;
        }
    }
}
