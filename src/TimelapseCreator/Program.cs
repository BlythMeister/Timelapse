using Accord.Video.FFMPEG;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;

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
            const string date = "2021-06-03";
            const string timezone = "BST";
            const bool rawOutput = false;
            const int bitRate = 20000;
            const int frameRate = 15;
            const bool removeRawImagesAfterProcessing = true;
            const bool includeAllImagesInVideo = false;
            const bool createVideo = true;
            TimeSpan? videoStartAt = new TimeSpan(7, 0, 0);
            TimeSpan? videoEndAt = new TimeSpan(19, 0, 0);

            try
            {
                TimestampImages(basePath, rawImageFolder, datedImageFolder, date, timezone, removeRawImagesAfterProcessing);
                Console.WriteLine("Images Dated");
                if (createVideo)
                {
                    CreateVideo(basePath, datedImageFolder, videoFolder, date, rawOutput, bitRate, frameRate, includeAllImagesInVideo, videoStartAt, videoEndAt);
                    Console.WriteLine("Video Created");
                }
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

        private static void CreateVideo(string basePath, string datedImageFolder, string videoFolder, string date, bool rawOutput, int bitRate, int frameRate, bool includeAllImagesInVideo, TimeSpan? startAt, TimeSpan? endAt)
        {
            var videoFolderPath = Path.Combine(basePath, videoFolder);
            string imagePath;
            string videoPath;

            if (includeAllImagesInVideo)
            {
                imagePath = Path.Combine(basePath, datedImageFolder);
                videoPath = rawOutput ? Path.Combine(videoFolderPath, "Complete.avi") : Path.Combine(videoFolderPath, "Complete.mp4");
            }
            else
            {
                var videoDate = date;

                if (startAt.HasValue || endAt.HasValue)
                {
                    if (startAt.HasValue)
                    {
                        videoDate += $"_{startAt.Value.Hours.ToString().PadLeft(2, '0')}{startAt.Value.Minutes.ToString().PadLeft(2, '0')}";
                    }
                    else
                    {
                        videoDate += "_0000";
                    }

                    if (endAt.HasValue)
                    {
                        videoDate += $"-{endAt.Value.Hours.ToString().PadLeft(2, '0')}{endAt.Value.Minutes.ToString().PadLeft(2, '0')}";
                    }
                    else
                    {
                        videoDate += "-2359";
                    }
                }

                imagePath = Path.Combine(basePath, datedImageFolder, date);
                videoPath = rawOutput ? Path.Combine(videoFolderPath, $"{videoDate}.avi") : Path.Combine(videoFolderPath, $"{videoDate}.mp4");
            }

            if (!Directory.Exists(imagePath))
            {
                Console.WriteLine($"Skipping timestamps as {imagePath} doesn't exist");
                return;
            }

            Directory.CreateDirectory(videoFolderPath);

            if (File.Exists(videoPath))
            {
                Console.WriteLine($"Video file {videoPath} already exists, so removing it");
                File.Delete(videoPath);
            }

            var files = Directory.GetFiles(imagePath, "*.jpg", SearchOption.AllDirectories).OrderBy(x => x).ToList();

            if (files.Any())
            {
                var codec = rawOutput ? VideoCodec.Raw : VideoCodec.Default;
                int height, width;
                using (var firstImage = Image.FromFile(files.First()))
                {
                    height = firstImage.Height;
                    width = firstImage.Width;
                }

                using (var writer = new VideoFileWriter())
                {
                    if (bitRate > 0)
                    {
                        writer.Open(videoPath, width, height, frameRate, codec, bitRate * 1000);
                    }
                    else
                    {
                        writer.Open(videoPath, width, height, frameRate, codec);
                    }

                    Console.WriteLine($"Creating video from {files.Count} images in {imagePath}.");

                    //Add all images as a frame in the video
                    foreach (var file in files)
                    {
                        if (!includeAllImagesInVideo)
                        {
                            var timestampText = file.Replace(imagePath, "").Substring(1).Replace(".jpg", "");
                            var photoTimeOfDay = DateTime.ParseExact(timestampText, "yyMMdd_HHmm", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal).TimeOfDay;

                            if (startAt.HasValue && photoTimeOfDay < startAt.Value)
                            {
                                continue;
                            }

                            if (endAt.HasValue && photoTimeOfDay > endAt.Value)
                            {
                                continue;
                            }
                        }

                        using (var bitmap = (Bitmap)Image.FromFile(file))
                        {
                            writer.WriteVideoFrame(bitmap);
                        }
                    }

                    writer.Close();
                }

                Console.WriteLine($"Video saved to {videoPath}");
            }
            else
            {
                Console.WriteLine($"No images in {imagePath}");
            }
        }
    }
}
