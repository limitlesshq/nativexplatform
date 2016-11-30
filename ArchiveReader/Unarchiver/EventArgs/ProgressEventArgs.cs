namespace Akeeba.Unarchiver.EventArgs
{
    /// <summary>
    /// Arguments for the progress event
    /// </summary>
    public class ProgressEventArgs : System.EventArgs
    {
        public ProgressEventArgs(extractionProgress a)
        {
            progress = a;
        }

        private extractionProgress _progress;

        public extractionProgress progress
        {
            get { return _progress;  }
            set { _progress = value; }
        }
    }
}
