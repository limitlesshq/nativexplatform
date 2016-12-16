namespace Akeeba.Unarchiver.EventArgs
{
    /// <summary>
    /// Arguments for the progress event
    /// </summary>
    public sealed class ProgressEventArgs : System.EventArgs
    {
        public ProgressEventArgs(ExtractionProgress a)
        {
            Progress = a;
        }

        public ExtractionProgress Progress { get; set; }
    }
}
