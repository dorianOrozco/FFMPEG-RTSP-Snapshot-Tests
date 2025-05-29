using Microsoft.Extensions.Configuration;
using RTSPTesting;
using System.Diagnostics;

public class Program
{
    public static async Task Main(string[] args)
    {
        // Allows us to use dot net user secrets
        var builder = new ConfigurationBuilder()
            .AddUserSecrets<Program>();
        IConfiguration config = builder.Build();

        // Retrieve credentials
        string username = config["RTSP:Username"];
        string password = config["RTSP:Password"];

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            Console.WriteLine("Credentials are missing.");
            return;
        }


        var cameras = new List<RTSPCamera>
        {
            new RTSPCamera { Name = "Camera Two", BaseUrl = "rtsp://192.168.1.112:554/Streaming/Channels/101" },
            new RTSPCamera { Name = "Camera Three", BaseUrl = "rtsp://192.168.1.113:554/Streaming/Channels/101" },
            new RTSPCamera { Name = "Camera Four", BaseUrl = "rtsp://192.168.1.114:554/Streaming/Channels/101" },
            new RTSPCamera { Name = "Camera Fiv", BaseUrl = "rtsp://192.168.1.115:554/Streaming/Channels/101" },
            new RTSPCamera { Name = "Camera Six", BaseUrl = "rtsp://192.168.1.116:554/Streaming/Channels/101" },
            new RTSPCamera { Name = "Camera Seven", BaseUrl = "rtsp://192.168.1.117:554/Streaming/Channels/101" },
            new RTSPCamera { Name = "Camera Eight", BaseUrl = "rtsp://192.168.1.118:554/Streaming/Channels/101" }
        };

        // Running processes concurrently.

        string transport = "tcp";

        Console.WriteLine($"\nUsing {transport.ToUpper()} transport...");
        Console.WriteLine("Capturing snapshots concurrently...\n");

        var tasks = cameras.Select(async cam =>
        {
            string fullUrl = cam.GetUrlWithCredentials(username, password);
            var (imageBytes, duration) = await CaptureSnapshotAsync(fullUrl, transport);

            if (imageBytes == null)
            {
                Console.WriteLine($"{cam.Name}: Snapshot failed.");
            }
            else
            {
                string filename = $"{cam.Name.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
                await File.WriteAllBytesAsync(filename, imageBytes);
                Console.WriteLine($"{cam.Name}: Saved as '{filename}' in {duration.TotalMilliseconds:F0}ms");
            }
        });
        await Task.WhenAll(tasks);
    }
    public static async Task<(byte[]?, TimeSpan)> CaptureSnapshotAsync(string url, string transport)
    {
        var ffmpegArgs = $"-hide_banner -loglevel error -rtsp_transport {transport} -i \"{url}\" " +
                         "-vf scale=1280:720 -vframes 1 -q:v 2 -f image2pipe -vcodec mjpeg -";

        Console.WriteLine(url);

        var startInfo = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = ffmpegArgs,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var sw = Stopwatch.StartNew();

        using var process = new Process { StartInfo = startInfo };
        using var ms = new MemoryStream();

        try
        {
            process.Start();
            await process.StandardOutput.BaseStream.CopyToAsync(ms);
            await process.WaitForExitAsync();
        }
        catch
        {
            return (null, sw.Elapsed);
        }

        sw.Stop();

        return ms.Length > 0 ? (ms.ToArray(), sw.Elapsed) : (null, sw.Elapsed);
    }
}
