namespace PatchKit.Unity.Patcher.AppData.Remote.Downloaders
{
    public interface IHttpWebRequestAdapter
    {
        IHttpWebResponseAdapter GetResponse();

        string Method { get; set; }

        int Timeout { get; set; }

        void AddRange(long start, long end);
    }
}