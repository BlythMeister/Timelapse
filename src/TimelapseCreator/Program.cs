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
            const string basePath = @"F:\DriveImages";
            const string rawImageFolder = "Raw";
            const string datedImageFolder = "Dated";
            const string videoFolder = "Video";
            const string date = "2021-05-23";
            const string timezone = "BST";
            const bool rawOutput = false;
            const int bitRate = 30000;
            const bool removeRawImagesAfterProcessing = true;

            try
            {
                TimestampImages(basePath, rawImageFolder, datedImageFolder, date, timezone, removeRawImagesAfterProcessing);
                CreateVideo(basePath, datedImageFolder, videoFolder, date, rawOutput, bitRate);
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

        private static void TimestampImages(string basePath, string rawImageFolder, string datedImageFolder, string date, string timezone, bool removeRawImagesAfterProcessing)
        {
            var rawPath = Path.Combine(basePath, rawImageFolder, date);
            var datedPath = Path.Combine(basePath, datedImageFolder, date);

            if (!Directory.Exists(rawPath))
            {
                Console.WriteLine($"Skipping timestamps as {rawPath} doesn't exist");
                return;
            }

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

            if (removeRawImagesAfterProcessing)
            {
                Console.WriteLine($"Removing {rawPath}");
                Directory.Delete(rawPath, true);
            }
        }

        private static void CreateVideo(string basePath, string datedImageFolder, string videoFolder, string date, bool rawOutput, int bitRate)
        {
            var datedPath = Path.Combine(basePath, datedImageFolder, date);
            var videoFolderPath = Path.Combine(basePath, videoFolder);
            var videoPath = rawOutput ? Path.Combine(videoFolderPath, $"{date}.avi") : Path.Combine(videoFolderPath, $"{date}.mp4");

            if (!Directory.Exists(datedPath))
            {
                Console.WriteLine($"Skipping timestamps as {datedPath} doesn't exist");
                return;
            }

            Directory.CreateDirectory(videoFolderPath);

            if (File.Exists(videoPath))
            {
                Console.WriteLine($"Video file {videoPath} already exists, so removing it");
                File.Delete(videoPath);
            }

            using (var writer = new VideoFileWriter())
            {
                //Hardcoded size and frame rate assuming input images came from bash script
                VideoCodec codec = rawOutput ? VideoCodec.Raw : VideoCodec.Default;
                if (bitRate > 0)
                {
                    writer.Open(videoPath, 1280, 720, 30, codec, bitRate * 1000);
                }
                else
                {
                    writer.Open(videoPath, 1280, 720, 30, codec);
                }

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
