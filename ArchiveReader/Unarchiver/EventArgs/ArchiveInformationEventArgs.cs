namespace Akeeba.Unarchiver.EventArgs
{
    /// <summary>
    /// Arguments for the archiveInformation event
    /// </summary>
    public class ArchiveInformationEventArgs: System.EventArgs
    {
        public ArchiveInformationEventArgs(archiveInformation a)
        {
            archiveInformation = a;
        }

        private archiveInformation _archiveInformation;

        public archiveInformation archiveInformation
        {
            get { return _archiveInformation;  }
            set { _archiveInformation = value; }
        }
    }
}
