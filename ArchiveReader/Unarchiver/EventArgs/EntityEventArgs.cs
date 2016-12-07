namespace Akeeba.Unarchiver.EventArgs
{
    /// <summary>
    /// Arguments for the entity event
    /// </summary>
    public class EntityEventArgs : System.EventArgs
    {
        public EntityEventArgs(EntityInformation a)
        {
            Information = a;
        }

        public EntityInformation Information { get; set; }
    }
}
