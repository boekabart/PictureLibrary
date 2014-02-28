using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Harvester.Properties;

namespace Harvester
{
    internal class Program
    {
        private static string _tempPath;
        private static string _targetPath;
        private static string _targetRoot;

        private static void Main( string[] args)
        {
            try
            {
                var runtime = TimeSpan.FromMinutes(int.Parse(args.FirstOrDefault() ?? "0"));
                var iterationTime = TimeSpan.FromSeconds(int.Parse(args.Skip(1).FirstOrDefault() ?? "10"));
                if (runtime.Ticks==0)
                    Info("Running once");
                else
                    Info("Running every {0} seconds for {1} minutes", iterationTime.TotalSeconds, runtime.TotalMinutes);

                Setup();

                var startTime = DateTime.UtcNow;
                var endTime = startTime + runtime;
                var iterationStartTime = startTime;

                do
                {
                    DoOneIteration();

                    var now = DateTime.UtcNow;
                    var iterationDuration = now - iterationStartTime;
                    var sleepTime = iterationTime - iterationDuration;

                    if (now + sleepTime >= endTime)
                        break;

                    var sleepInMs = (int)sleepTime.TotalMilliseconds;
                    if (sleepInMs > 0)
                    {
                        Info("Sleeping {0} ms", sleepInMs);
                        Thread.Sleep(sleepInMs);
                    }
                    iterationStartTime = DateTime.UtcNow;
                } while (DateTime.UtcNow < endTime);
            }
            catch (Exception e)
            {
                Error("Something went wrong", e);
            }
        }

        private static void DoOneIteration()
        {
            try
            {
                RetryTempFolder();

                var movedFiles = Harvest();
                var movedFilesFromOtherDrive = movedFiles.Any(DifferentRoot);
                if (movedFilesFromOtherDrive)
                {
                    // Double Check If Files Left
                    if (!GetFilesToQueue().Any(DifferentRoot))
                        PlayCompletedSound();
                }
            }
            catch (Exception e)
            {
                Error("Something went wrong", e);
            }
        }

        private static IEnumerable<string> Harvest()
        {
            var filesToQueue = GetFilesToQueue();
            return filesToQueue.Where(TryMoveFileToTargetViaTemp).ToArray();
        }

        private static void RetryTempFolder()
        {
            var inboxLeftovers = TryEnumerateFiles(_tempPath);
            foreach (var file in inboxLeftovers.Where(Helpers.IsSupportedFile))
                TryMoveFile(file, ReplaceDirectory(file, _targetPath));
        }

        private static void Setup()
        {
            _tempPath = Settings.Default.Temp;
            Directory.CreateDirectory(_tempPath);
            _targetPath = Settings.Default.Target;
            Directory.CreateDirectory(_targetPath);
            _targetRoot = Path.GetPathRoot(_targetPath);
        }

        private static void PlayCompletedSound()
        {
            new SoundPlayer("harvested.wav").PlaySync();
        }

        private static bool DifferentRoot(string file)
        {
            return !Path.GetPathRoot(file).Equals(_targetRoot, StringComparison.InvariantCultureIgnoreCase);
        }

        private static IEnumerable<string> GetFilesToQueue()
        {
            var sources = Settings.Default.Sources.OfType<string>().ToArray();
            var recSources = Settings.Default.RecursiveSources.OfType<string>().ToArray();
            var filesEnum =
                sources.SelectMany(TryEnumerateFiles)
                    .Concat(recSources.SelectMany(TryEnumerateFilesRecursive));

            var filesToQueue = filesEnum.Where(Helpers.IsSupportedFile);
            return filesToQueue;
        }

        private static IEnumerable<string> TryEnumerateFilesRecursive(string arg)
        {
            try
            {
                return Directory.EnumerateFiles(arg, "*", SearchOption.AllDirectories);
            }
            catch (Exception e)
            {
                Error("Couldn't enumerate files in {0}", e, arg);
                return new string[0];
            }
        }

        private static IEnumerable<string> TryEnumerateFiles(string arg)
        {
            try
            {
                return Directory.EnumerateFiles(arg);
            }
            catch (Exception e)
            {
                Error("Couldn't enumerate files in {0}", e, arg);
                return new string[0];
            }
        }

        private static bool TryMoveFileToTargetViaTemp(string fullPath)
        {
            var tempDest = ReplaceDirectory(fullPath, _tempPath);
            var finalDest = ReplaceDirectory(fullPath, _targetPath);
            if (!TryMoveFile(fullPath, tempDest))
                return false;
            TryMoveFile(tempDest, finalDest);
            return true;
        }

        private static string ReplaceDirectory(string fullPath, string newDirectory)
        {
            return Path.Combine(newDirectory, Path.GetFileName(fullPath));
        }

        private static bool TryMoveFile(string fullPath, string tempDest)
        {
            try
            {
                File.Move(fullPath, tempDest);
                Info("Moved file {0} to {1}", fullPath, tempDest);
                return true;
            }
            catch (Exception e)
            {
                Error("Failed to move file {0} to {1}", e, fullPath, tempDest);
                return false;
            }
        }

        private static void Log(string format, params object[] x)
        {
            var msg = string.Format(format, x);
            Console.WriteLine(msg);
            if (!string.IsNullOrWhiteSpace(Settings.Default.LogFile))
                TryWriteLine(Settings.Default.LogFile, msg);
        }

        private static void TryWriteLine(string logFile, string msg)
        {
            try
            {
                File.AppendAllLines(logFile, new[] {string.Format("{0:yyyy-MM-dd HH:mm:ss} {1}",DateTime.Now, msg)});
            }
            catch
            {
                Console.WriteLine("Failed to log to {0}", logFile);
            }
        }

        private static void Error(string _msg, Exception exception, params object[] x)
        {
            var msg = string.Format(_msg, x);
            Log("ERROR: {0} [{1}]", msg, exception == null ? String.Empty : exception.Message);
        }

        private static void Info(string _msg, params object[] x)
        {
            var msg = string.Format(_msg, x);
            Log("INFO : {0}", msg);
        }
    }
}
