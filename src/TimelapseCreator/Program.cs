using Accord.Video.FFMPEG;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;

namespace TimelapseCreator
{
    internal class Program
    {
        private static void Main(string[] _)
        {
            var basePath = @"F:\DriveImages";
            var rawImageFolder = "Raw";
            var datedImageFolder = "Dated";
            var videoFolder = "Video";
            var date = "2021-05-17";
            var timezone = "BST";

            TimestampImages(basePath, rawImageFolder, datedImageFolder, date, timezone);
            CreateVideo(basePath, datedImageFolder, videoFolder, date);
            Console.ReadLine();
        }

        private static void TimestampImages(string basePath, string rawImageFolder, string datedImageFolder, string date, string timezone)
        {
            var rawPath = Path.Combine(basePath, rawImageFolder, date);
            var datedPath = Path.Combine(basePath, datedImageFolder, date);

            if (Directory.Exists(datedPath))
            {
                Directory.Delete(datedPath, true);
            }
            Directory.CreateDirectory(datedPath);

            Console.WriteLine($"Outputting images from {rawPath} with timestamp");
            foreach (var file in Directory.GetFiles(rawPath, "*.*", SearchOption.AllDirectories))
            {
                //Load the Image to be written on.
                using (var bitMapImage = new Bitmap(file))
                {
                    var graphicImage = Graphics.FromImage(bitMapImage);

                    var timestampText = file.Replace(rawPath, "").Substring(1).Replace(".jpg", "");
                    var timestamp = DateTime.ParseExact(timestampText, "yyMMdd_HHmm", CultureInfo.InvariantCulture);

                    //Draw rectangle
                    graphicImage.FillRectangle(new SolidBrush(Color.Black), 895, 675, 500, 100);
                    graphicImage.DrawString($"{timestamp:yyyy-MM-dd HH:mm} {timezone}", new Font("Consolas", 24, FontStyle.Bold), new SolidBrush(Color.White), 900, 680);

                    //Save the new image
                    var newPath = file.Replace(rawPath, datedPath);
                    Console.WriteLine($"Saving: {newPath}");
                    bitMapImage.Save(newPath, ImageFormat.Jpeg);
                }
            }
        }

        private static void CreateVideo(string basePath, string datedImageFolder, string videoFolder, string date)
        {
            var datedPath = Path.Combine(basePath, datedImageFolder, date);
            var videoFolderPath = Path.Combine(basePath, videoFolder);
            var videoPath = Path.Combine(videoFolderPath, $"{date}.avi");

            if (File.Exists(videoPath))
            {
                File.Delete(videoPath);
            }

            Directory.CreateDirectory(videoFolderPath);

            var files = Directory.GetFiles(datedPath);

            int frameHeight;
            int frameWidth;
            var frameRate = 30;

            using (var firstImage = Image.FromFile(files[0]))
            {
                frameHeight = firstImage.Height;
                frameWidth = firstImage.Width;
            }

            Console.WriteLine($"Creating video from images in {datedPath}.  Size: {frameHeight}x{frameWidth} @ {frameRate}fps");

            using (var writer = new VideoFileWriter())
            {
                writer.Open(videoPath, frameWidth, frameHeight, frameRate, VideoCodec.Raw);

                foreach (var file in files)
                {
                    using (var bitmap = (Bitmap)Image.FromFile(file))
                    {
                        writer.WriteVideoFrame(bitmap);
                    }
                }

                writer.Close();
            }

            Console.WriteLine($"Video created {videoPath}");
        }
    }
}
