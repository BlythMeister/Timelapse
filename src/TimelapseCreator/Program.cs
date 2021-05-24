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
            //These could come from arguments
            var basePath = @"F:\DriveImages";
            var rawImageFolder = "Raw";
            var datedImageFolder = "Dated";
            var videoFolder = "Video";
            var date = "2021-05-17";
            var timezone = "BST";

            try
            {
                TimestampImages(basePath, rawImageFolder, datedImageFolder, date, timezone);
                CreateVideo(basePath, datedImageFolder, videoFolder, date);
                Console.WriteLine("Video Created");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                Console.WriteLine("Press Enter To Close...");
                Console.ReadLine();
            }
        }

        private static void TimestampImages(string basePath, string rawImageFolder, string datedImageFolder, string date, string timezone)
        {
            var rawPath = Path.Combine(basePath, rawImageFolder, date);
            var datedPath = Path.Combine(basePath, datedImageFolder, date);

            //Clean output directory
            if (Directory.Exists(datedPath))
            {
                Console.WriteLine($"Directory {datedPath} already exists, so cleaning it");
                Directory.Delete(datedPath, true);
            }
            Directory.CreateDirectory(datedPath);

            Console.WriteLine($"Outputting images from {rawPath} with timestamp");
            foreach (var file in Directory.GetFiles(rawPath, "*.jpg", SearchOption.TopDirectoryOnly))
            {
                //Load the Image to be written on.
                using (var bitMapImage = new Bitmap(file))
                using (var graphicImage = Graphics.FromImage(bitMapImage))
                {
                    var timestampText = file.Replace(rawPath, "").Substring(1).Replace(".jpg", "");
                    var timestamp = DateTime.ParseExact(timestampText, "yyMMdd_HHmm", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal);

                    //Draw rectangle
                    graphicImage.FillRectangle(new SolidBrush(Color.Black), 895, 675, 500, 100);
                    //Write timestamp
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

            Directory.CreateDirectory(videoFolderPath);

            if (File.Exists(videoPath))
            {
                Console.WriteLine($"Video file {videoPath} already exists, so removing it");
                File.Delete(videoPath);
            }

            using (var writer = new VideoFileWriter())
            {
                //Hardcoded size and framerate assuming input images came from bash script
                writer.Open(videoPath, 1280, 720, 30, VideoCodec.Raw);

                var files = Directory.GetFiles(datedPath);

                Console.WriteLine($"Creating video from {files.Length} images in {datedPath}.");

                //Add all images as a frame in the video
                foreach (var file in files)
                {
                    using (var bitmap = (Bitmap)Image.FromFile(file))
                    {
                        writer.WriteVideoFrame(bitmap);
                    }
                }

                writer.Close();
            }

            Console.WriteLine($"Video saved to {videoPath}");
        }
    }
}
