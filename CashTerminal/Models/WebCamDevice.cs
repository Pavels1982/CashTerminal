namespace WebCam
{
    public class WebCamDevice
    {
        public string Name { get; set; }
        public string Moniker { get; set; }

        public WebCamDevice(string name, string moniker)
        {
            this.Name = name;
            this.Moniker = moniker;
        }

    }
}
