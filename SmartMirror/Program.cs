using System.IO;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace SmartMirror
{
    static class IoHelpers
    {
        public static IEnumerable<string> EnumerateFiles(this string path,
                               string[] searchPatterns,
                               SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            return searchPatterns.AsParallel()
                   .SelectMany(searchPattern =>
                          Directory.EnumerateFiles(path, searchPattern, searchOption));
        }

        public static IEnumerable<string> WithThumbnails(this IEnumerable<string> paths)
        {
            return paths.SelectMany(fn => fn.WithCompanions(new[] {@".THM"}));
        }

        public static IEnumerable<string> WithCompanions(this string path, string[] companionExtensions)
        {
            return
                new[] {path}.Concat(companionExtensions.Select(ext => Path.ChangeExtension(path, ext))
                    .Where(File.Exists));
        }

        public static IEnumerable<string> GetStarredFiles(this string dir)
        {
            var picasaInis = Directory.EnumerateFiles(dir, @".picasa.ini", SearchOption.AllDirectories);
            return picasaInis.SelectMany(StarredFromPicasaIni);
        }

        private static IEnumerable<string> StarredFromPicasaIni(this string picasaIniPath)
        {
            var dir = Path.GetDirectoryName(picasaIniPath);
            try
            {
                var parsedIni = new IniParser(picasaIniPath);
                var filesInIni = parsedIni.EnumSections();
                var starredFiles = filesInIni.Where(sec => IsStarred(sec, parsedIni));
                var fullPahts = starredFiles.Select(fn => Path.Combine(dir, fn));
                var existingStarredFiles = fullPahts.Where(File.Exists).ToArray();
                return !existingStarredFiles.Any() ? new String[0] : new[] {picasaIniPath}.Concat(existingStarredFiles);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Failed to parse {0}: {1}", picasaIniPath, e);
                return new string[0];
            }
        }

        private static bool IsStarred(string sec, IniParser parsedIni)
        {
            var starValue = parsedIni.TryGetSetting(sec, "star");
            return starValue != null && starValue.Equals("yes", StringComparison.InvariantCultureIgnoreCase);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var srcDir = @"D:\Fotos\";

            var videoDestDir = @"D:\FotoMirrors\Videos\";
            MirrorVideos(srcDir, videoDestDir);

            var starredDestDir = @"D:\FotoMirrors\Starred\";
            MirrorStarred(srcDir, starredDestDir);

            var fsm = new FileSystemWatcher();
            fsm.Filter = ".picasa.ini";
            fsm.Path = srcDir;
            fsm.IncludeSubdirectories = true;
        }

        private static void MirrorVideos(string srcDir, string destDir)
        {
            Console.WriteLine("Mirroring Videos from {0} to {1}", srcDir, destDir);
            var mp4Paths = srcDir.EnumerateFiles(new[] {@"*.M4V"}, SearchOption.AllDirectories);
            var mp4PathsWithThms = mp4Paths.WithThumbnails();
            UpdateMirror(mp4PathsWithThms, srcDir, destDir);
        }

        private static void MirrorStarred(string srcDir, string destDir)
        {
            Console.WriteLine("Mirroring Starred Files from {0} to {1}", srcDir, destDir);
            var starredPaths = srcDir.GetStarredFiles();
            UpdateMirror(starredPaths, srcDir, destDir);
        }

        private static void UpdateMirror(IEnumerable<string> mp4PathsWithThms, string srcDir, string destDir)
        {
            var relativePaths = mp4PathsWithThms.Select(path => path.Substring(srcDir.Length));
            var existingRelativePaths = new HashSet<string>();
            foreach (var relPath in relativePaths)
            {
                existingRelativePaths.Add(relPath.ToLowerInvariant());
                var srcPath = Path.Combine(srcDir, relPath);
                var dstPath = Path.Combine(destDir, relPath);
                HardlinkIfNotExists(srcPath, dstPath);
            }

            var existingFilesInTarget = Directory.EnumerateFiles(destDir, "*.*", SearchOption.AllDirectories);
            var relativeLowerCaseExistinFilesInTarget =
                existingFilesInTarget.Select(path => path.Substring(destDir.Length).ToLowerInvariant());
            var surplusRelativeFiles =
                relativeLowerCaseExistinFilesInTarget.Where(relPath => !existingRelativePaths.Contains(relPath));
            foreach (var relPathToDelete in surplusRelativeFiles)
            {
                var fullPathToDelete = Path.Combine(destDir, relPathToDelete);
                fullPathToDelete.DeleteFile();
                Console.WriteLine("Deleted {0}", fullPathToDelete);
            }
            foreach (var dir in Directory.EnumerateDirectories(destDir, "*.*", SearchOption.AllDirectories))
            {
                if (dir.TryDeleteDirectory())
                    Console.WriteLine("Deleted directory {0}");
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
            else
                Console.WriteLine("Could not create {0}", dstPath);
        }
    }
}
