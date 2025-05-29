namespace RTSPTesting
{
    public class RTSPCamera
    {
        public string Name { get; set; }
        public string BaseUrl { get; set; }

        public string GetUrlWithCredentials(string username, string password)
        {
            var uri = new Uri(BaseUrl);

            var builder = new UriBuilder(uri)
            {
                UserName = Uri.EscapeDataString(username),
                Password = Uri.EscapeDataString(password)
            };

            return builder.Uri.ToString();
        }
    }


}
