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
    static class IoHelpers
    {
        public static IEnumerable<string> EnumerateFilesIfDirExists(string destDir, string searchPattern = "*.*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            return Directory.Exists(destDir)
                ? Directory.EnumerateFiles(destDir, searchPattern, searchOption)
                : new string[0];
        }

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

        public static IObservable<string> ToObservableSimple(this FileSystemWatcher src)
        {
            var sources = new[] 
                    { 
                        Observable.FromEvent<FileSystemEventHandler, FileSystemEventArgs>( handler => (sender, e) => handler(e), eh => src.Deleted += eh,  eh => src.Deleted -= eh ),
                        Observable.FromEvent<FileSystemEventHandler, FileSystemEventArgs>( handler => (sender, e) => handler(e), eh => src.Created += eh,  eh => src.Created -= eh ),
                        Observable.FromEvent<FileSystemEventHandler, FileSystemEventArgs>( handler => (sender, e) => handler(e), eh => src.Changed += eh,  eh => src.Changed -= eh ),
                    };
            return sources.Merge().Select(ev => ev.FullPath);
        }

        public static IObservable<string> ObserveFileSystem( this string srcDir, string filter)
        {
            return Observable.Create<string>(
                subscriber =>
                {
                    var fsm = new FileSystemWatcher
                    {
                        Filter = filter,
                        Path = srcDir,
                        IncludeSubdirectories = true
                    };
                    fsm.ToObservableSimple().Subscribe(subscriber);
                    fsm.EnableRaisingEvents = true;
                    return fsm;
                });
        }

        public static IObservable<T> ThrottlePerValue<T>(this IObservable<T> src, TimeSpan throttleValue)
        {
            return src.GroupBy(_ => _).SelectMany(gr => gr.Throttle(throttleValue));
        }

        public static IObservable<T> Prefix<T>(this IObservable<T> src, T firstValue)
        {
            return new[] {firstValue}.ToObservable().Concat(src);
        }
    }

    class Program
    {
        private static void Main(string[] args)
        {
            var srcDir = @"D:\Fotos\";
            var videoDestDir = @"D:\FotoMirrors\Videos\";
            var starredDestDir = @"D:\FotoMirrors\Starred\";

            var videoObservable = srcDir.ObserveFileSystem("*.M4V")
                .Select(Path.GetDirectoryName)
                .ThrottlePerValue(TimeSpan.FromSeconds(5)).Prefix(srcDir);

            var starObservable = srcDir.ObserveFileSystem(".picasa.ini")
                .Select(Path.GetDirectoryName)
                .ThrottlePerValue(TimeSpan.FromSeconds(5)).Prefix(srcDir);

            using (
                videoObservable.Subscribe(
                    pad => UpdateMirror(CollectVideoCandidates, pad, pad.Replace(srcDir, videoDestDir))))
            using (
                starObservable.Subscribe(
                    pad => UpdateMirror(CollectStarredCandidates, pad, pad.Replace(srcDir, starredDestDir))))
            {
                while (!Console.KeyAvailable)
                    Thread.Sleep(100);
            }
        }

        private static IEnumerable<string> CollectVideoCandidates(string dir)
        {
            return dir.EnumerateFiles(new[] {@"*.M4V"}, SearchOption.AllDirectories).WithThumbnails();
        }

        private static IEnumerable<string> CollectStarredCandidates(string dir)
        {
            return dir.GetStarredFiles();
        }

        private static void UpdateMirror(Func<string, IEnumerable<string>> srcToIncludeListFunc, string srcDir,
            string destDir)
        {
            UpdateMirror(srcToIncludeListFunc(srcDir), srcDir, destDir);
        }

        private static void UpdateMirror(IEnumerable<string> mp4PathsWithThms, string srcDir, string destDir)
        {
            if (!srcDir.EndsWith(Path.DirectorySeparatorChar.ToString(CultureInfo.InvariantCulture)))
                srcDir += Path.DirectorySeparatorChar;
            if (!destDir.EndsWith(Path.DirectorySeparatorChar.ToString(CultureInfo.InvariantCulture)))
                destDir += Path.DirectorySeparatorChar;

            var relativePaths = mp4PathsWithThms.Select(path => path.Substring(srcDir.Length));
            var existingRelativePaths = new HashSet<string>();
            foreach (var relPath in relativePaths)
            {
                existingRelativePaths.Add(relPath.ToLowerInvariant());
                var srcPath = Path.Combine(srcDir, relPath);
                var dstPath = Path.Combine(destDir, relPath);
                HardlinkIfNotExists(srcPath, dstPath);
            }

            var existingFilesInTarget = IoHelpers.EnumerateFilesIfDirExists(destDir, "*.*", SearchOption.AllDirectories);

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
                if (dir.TryDeleteDirectoryAndParents())
                    Console.WriteLine("Deleted directory {0}", dir);
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
