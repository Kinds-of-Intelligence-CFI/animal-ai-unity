namespace Operations
{
    /// <summary>
    /// A dummy operation that does nothing
    /// </summary>
    public class NoneOperation : Operation
    {

        public void Initialize(AttachedObjectDetails details)
        {
            attachedObjectDetails = details;
        }

        public override void execute() { }
    }
}