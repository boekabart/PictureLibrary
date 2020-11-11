using System.Globalization;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace SmartMirror
{
    class Program
    {
        private static void Main(string[] args)
        {
        var srcDir = @"D:\Fotos";
            var videoDestDir = @"D:\FotoMirrors\Videos";
            var starredDestDir = @"D:\FotoMirrors\Starred";
            var starredDestDir2 = @"I:\FotoMirrors\Starred";
            var personDestDir = @"D:\FotoMirrors\People";

            var persons = new[]
            {
                "Sjoerd de Boer",
                "Pieter de Boer",
                "Simon de Boer",
                "Mathijs de Boer",
                "Bart de Boer",
                "Maaike de Boer",
                "Anne-Riet de Boer",
                "Victoria Flutur",
                "Liviu Flutur",
                "Michaela Flutur",
                "Lorelei de Boer",
                "Sebbe de Boer",
                "Sophie de Boer",
                "Laura Candidatu",
                "Bogdan Flutur",
                "Célestine de Boer"
            }.ToObservable();

            var personData =
                persons.Select(
                    n => new {Name = n, Person = PicasaPerson.TryGetPerson(n), Path = Path.Combine(personDestDir, n)})
                    .Where(set => set.Person != null);

            var videoObservable = srcDir.ObserveFileSystem("*.M4V")
                .Select(Path.GetDirectoryName)
                .ThrottlePerValue(TimeSpan.FromSeconds(15))
                .StartWith(srcDir);

            var picasaObservable = srcDir.ObserveFileSystem(".picasa.ini")
                .Select(Path.GetDirectoryName)
                .ThrottlePerValue(TimeSpan.FromSeconds(15))
                .StartWith(srcDir).Publish();

            var personDisposable = new CompositeDisposable();
            personDisposable.Add(personData.Subscribe(
                on => personDisposable.Add(picasaObservable.Subscribe(
                    pad =>
                        UpdateMirror(ini => ini.GetFilesWithPerson(on.Person), pad, on.Path, srcDir)))));

            using (
                videoObservable.Subscribe(
                    pad => UpdateMirror(CollectVideoCandidates, pad, videoDestDir, srcDir)))
            using (
                picasaObservable.Subscribe(
                    pad => UpdateMirror(CollectStarredCandidates, pad, starredDestDir, srcDir)))
            using (
                picasaObservable.Subscribe(
                    pad => UpdateMirror(CollectStarredCandidates, pad, starredDestDir2, srcDir)))
            using (personDisposable)
            using (picasaObservable.Connect())
            {
                while (!Console.KeyAvailable)
                    Thread.Sleep(100);
            }
        }

        private static string GetRelativeMirrorPath_YearMonth(string srcFullPath, string srcRelDir)
        {
            var fileTime = GetFileYearMonth(srcFullPath);
            var dir = string.Format(@"{0:yyyy}\{0:yyyyMM - MMMM yyyy}", fileTime);
            return Path.Combine(dir, Path.GetFileName(srcRelDir));
        }

        private static DateTime GetFileYearMonth(string pad)
        {
            var fileName = Path.GetFileName(pad);
            try
            {
                var year = int.Parse(fileName.Substring(0, 4));
                var month = int.Parse(fileName.Substring(4, 2));
                if (year >= 1900 && year < 2100 && month >= 1 && month <= 12)
                    return new DateTime(year, month, 1);
            }
            catch
            {
            }
            try
            {
                return new FileInfo(pad).LastWriteTime;
            }
            catch
            {
                return new DateTime(1950, 1, 1);
            }
        }

        private static IEnumerable<string> CollectVideoCandidates(string dir)
        {
            try
            {
                return dir.EnumerateFiles(new[] {@"*.M4V"}, SearchOption.AllDirectories).WithThumbnails();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error in CollectVideoCandidates: {0}", e.Message);
                return new String[0];
            }
        }

        private static IEnumerable<string> CollectStarredCandidates(string dir)
        {
            return dir.GetStarredFiles().WithThumbnails();
        }

        private static void UpdateMirror(Func<string, IEnumerable<string>> srcToIncludeListFunc, string srcDir,
            string destDir, string srcRoot)
        {
            UpdateMirror(srcToIncludeListFunc(srcDir), srcDir, destDir, srcRoot);
        }

        private static void UpdateMirror(IEnumerable<string> mp4PathsWithThms, string srcDir, string destRoot, string srcRoot)
        {
            if (!srcRoot.EndsWith(Path.DirectorySeparatorChar.ToString(CultureInfo.InvariantCulture)))
                srcRoot += Path.DirectorySeparatorChar;
            if (!srcDir.EndsWith(Path.DirectorySeparatorChar.ToString(CultureInfo.InvariantCulture)))
                srcDir += Path.DirectorySeparatorChar;

            bool doDelete = srcRoot.Equals(srcDir, StringComparison.InvariantCultureIgnoreCase);

            if (!destRoot.EndsWith(Path.DirectorySeparatorChar.ToString(CultureInfo.InvariantCulture)))
                destRoot += Path.DirectorySeparatorChar;

            var relativePaths = mp4PathsWithThms.Select(path => path.Substring(srcDir.Length));
            var triplets = relativePaths.Select(
                relPath =>
                    new
                    {
                        RelSrc = relPath,
                        FullSrc = Path.Combine(srcDir, relPath),
                        RelDst = GetRelativeMirrorPath_YearMonth(Path.Combine(srcDir, relPath), relPath)
                    }).ToArray();

            var relativePathThatShouldExistInDestination = new HashSet<string>(triplets.Select(tr => tr.RelDst.ToLowerInvariant()));

            foreach (var trip in triplets)
            {
                var dstPath = Path.Combine(destRoot, trip.RelDst);
                HardlinkIfNotExists(trip.FullSrc, dstPath);
            }

            if (doDelete)
            {
                var existingFilesInTarget = IoHelpers.EnumerateFilesIfDirExists(destRoot, "*.*",
                    SearchOption.AllDirectories);

                var relativeLowerCaseExistinFilesInTarget =
                    existingFilesInTarget.Select(path => path.Substring(destRoot.Length).ToLowerInvariant());
                var surplusRelativeFiles =
                    relativeLowerCaseExistinFilesInTarget.Except(relativePathThatShouldExistInDestination);

                foreach (var relPathToDelete in surplusRelativeFiles)
                {
                    var fullPathToDelete = Path.Combine(destRoot, relPathToDelete);
                    TryDeleteFile(fullPathToDelete);
                }
            }

            foreach (var dir in IoHelpers.EnumerateFilesIfDirExists(destRoot, "*.*", SearchOption.AllDirectories))
            {
                if (dir.TryDeleteDirectoryAndParents())
                    Console.WriteLine("Deleted directory {0}", dir);
            }
        }

        private static void TryDeleteFile(string fullPathToDelete)
        {
            try
            {
                fullPathToDelete.DeleteFile();
                Console.WriteLine("Deleted {0}", fullPathToDelete);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Error deleting {0}: {1}", fullPathToDelete, e.Message);
            }
        }

        private static void HardlinkIfNotExists(string srcPath, string dstPath)
        {
            if (dstPath.FileExists())
            {
                //Console.WriteLine("Existing {0}", dstPath);
                return;
            }
            if (srcPath.TryHardLinkFile(dstPath))
                Console.WriteLine("Created {0}", dstPath);
            else if (srcPath.TryCopyFile(dstPath))
                Console.WriteLine("Copied {0}", dstPath);
            else
                Console.WriteLine("Could not create {0}", dstPath);
        }
    }
}
