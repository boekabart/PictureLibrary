using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;

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
                    EnumerateFilesIfDirExists(path, searchPattern, searchOption));
        }

        public static IEnumerable<string> WithThumbnails(this IEnumerable<string> paths)
        {
            return paths.SelectMany(fn => fn.WithCompanions(new[] {@".THM", @".THV"}));
        }

        public static IEnumerable<string> WithCompanions(this string path, string[] companionExtensions)
        {
            return
                new[] {path}.Concat(companionExtensions.Select(ext => Path.ChangeExtension(path, ext))
                    .Where(File.Exists));
        }

        public static IEnumerable<string> GetStarredFiles(this string dir)
        {
            try
            {
                var picasaInis = Directory.EnumerateFiles(dir, @".picasa.ini", SearchOption.AllDirectories);
                return picasaInis.SelectMany(StarredFromPicasaIni);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: {0}", e.Message);
                return new String[0];
            }
        }

        private static IEnumerable<string> StarredFromPicasaIni(this string picasaIniPath)
        {
            var dir = Path.GetDirectoryName(picasaIniPath);
            try
            {
                var parsedIni = new ParsedIni(picasaIniPath);
                var filesInIni = parsedIni.EnumSections();
                var starredFiles = filesInIni.Where(sec => IsStarred(sec, parsedIni));
                var fullPahts = starredFiles.Select(fn => Path.Combine(dir, fn));
                var existingStarredFiles = fullPahts.Where(File.Exists).ToArray();
                return existingStarredFiles;
                //return !existingStarredFiles.Any() ? new String[0] : new[] {picasaIniPath}.Concat(existingStarredFiles);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Failed to parse {0}: {1}", picasaIniPath, e);
                return new string[0];
            }
        }

        public static IEnumerable<string> GetFilesWithPerson(this string dir, PicasaPerson picasaPerson)
        {
            try
            {
                var picasaInis = Directory.EnumerateFiles(dir, @".picasa.ini", SearchOption.AllDirectories);
                return picasaInis.SelectMany( ini => ini.WithPersonFromPicasaIni(picasaPerson));
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: {0}", e.Message);
                return new String[0];
            }
        }

        private static IEnumerable<string> WithPersonFromPicasaIni(this string picasaIniPath, PicasaPerson picasaPerson)
        {
            var dir = Path.GetDirectoryName(picasaIniPath);
            try
            {
                var parsedIni = new ParsedIni(picasaIniPath);
                var filesInIni = parsedIni.EnumSections();
                var filesWithPerson = filesInIni.Where(sec => HasPerson(sec, parsedIni, picasaPerson));
                var fullPathsWithPerson = filesWithPerson.Select(fn => Path.Combine(dir, fn));
                var existingPersonFiles = fullPathsWithPerson.Where(File.Exists).ToArray();
                return existingPersonFiles;
                //return !existingPersonFiles.Any() ? new String[0] : new[] { picasaIniPath }.Concat(existingPersonFiles);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Failed to parse {0}: {1}", picasaIniPath, e);
                return new string[0];
            }
        }

        private static bool HasPerson(string sec, ParsedIni parsedIni, PicasaPerson picasaPerson)
        {
            var facesValue = parsedIni.TryGetSetting(sec, "faces");
            if (facesValue == null)
                return false;
            var personIds = facesValue.Split(';')
                .SelectMany(pairs => pairs.Split(',').Skip(1).Take(1));
            return personIds.Intersect(picasaPerson.FocusIds).Any();
        }

        private static bool IsStarred(string sec, ParsedIni picasaIni)
        {
            var starValue = picasaIni.TryGetSetting(sec, "star");
            return starValue != null && starValue.Equals("yes", StringComparison.InvariantCultureIgnoreCase);
        }

        public static IObservable<string> ToObservableSimple(this FileSystemWatcher src)
        {
            var name = string.Format("FSW({0})", src.Filter);
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
                    fsm.EnableRaisingEvents = true;
                    return new CompositeDisposable(fsm, fsm.ToObservableSimple().Subscribe(subscriber));
                });
        }

        public static IObservable<T> ThrottlePerValue<T>(this IObservable<T> src, TimeSpan throttleValue)
        {
            return src.GroupByUntil(v => v, gr => gr.Throttle(throttleValue))
                .SelectMany(gr => gr.LastAsync());
        }
    }
}