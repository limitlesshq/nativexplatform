namespace Akeeba.Unarchiver.EventArgs
{
    /// <summary>
    /// Arguments for the entity event
    /// </summary>
    public class EntityEventArgs : System.EventArgs
    {
        public EntityEventArgs(entityInformation a)
        {
            information = a;
        }

        private entityInformation _information;

        public entityInformation information
        {
            get { return _information;  }
            set { _information = value; }
        }
    }
}
