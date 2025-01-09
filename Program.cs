namespace docktube;

using System;
using System.Reflection;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Videos.Streams;
using static System.Net.Mime.MediaTypeNames;
using static System.Net.WebRequestMethods;
using Xabe.FFmpeg.Downloader;
using Xabe.FFmpeg;
using System.IO;

class Program
{
    static async Task Main(string[] args)
    {
        var youtube = new YoutubeClient();
        var channelUrl = "https://youtube.com/channel/UCLe_q9axMaeTbjN0hy1Z9xA";
        var channel = await youtube.Channels.GetAsync(channelUrl);

        var title = channel.Title;
        var videos = await youtube.Channels.GetUploadsAsync(channelUrl).Where(x => x.Duration > TimeSpan.FromMinutes(30));

        var latest = videos.First();
        var vidTitle = latest.Title; // "Collections - Blender 2.80 Fundamentals"
        var author = latest.Author.ChannelTitle; // "Blender"
        var duration = latest.Duration; // 00:07:20

        var streamManifest = await youtube.Videos.Streams.GetManifestAsync(latest.Url);

        var audioStreamInfo = streamManifest
            .GetAudioStreams()
            .Where(s => s.Container == Container.Mp4)
            .GetWithHighestBitrate();

        var workingDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        var audio = await youtube.Videos.Streams.GetAsync(audioStreamInfo);
        var audioFile = Path.Combine(workingDir, $"{vidTitle}.mp4");
        await youtube.Videos.Streams.DownloadAsync(audioStreamInfo, audioFile);

        FFmpeg.SetExecutablesPath(workingDir);
        await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official, workingDir);

        string output = Path.ChangeExtension(audioFile, "mp3");

        var podcastFile = Path.Combine(workingDir, output);
        var conversion = await FFmpeg.Conversions.FromSnippet.Convert(audioFile, podcastFile);
        await conversion.Start();

        // Get folder Node.
        string smbPath = Environment.GetEnvironmentVariable("SMB_PATH");
        string user = Environment.GetEnvironmentVariable("SMB_USER");
        string password = Environment.GetEnvironmentVariable("SMB_PASSWORD");
        var folder = await EzSmb.Node.GetNode(smbPath, user, password);
        var fs = System.IO.File.Open(podcastFile, FileMode.Open, FileAccess.Read, FileShare.None);
        var ok = await folder.Write(fs, $"{vidTitle}.mp3");
        Console.WriteLine($"File operation: {ok}");
    }
}