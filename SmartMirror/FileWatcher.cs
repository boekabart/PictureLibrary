using System;
using System.IO;
using System.Reactive.Linq;

namespace SmartMirror
{/*
    public class FileWatcher
    {
        public static IObservable<FileChangedEvent> ObserveFolderChanges(string path, string filter, TimeSpan throttle)
        {
            return Observable.Create<FileChangedEvent>(
                observer =>
                {
                    var fileSystemWatcher = new FileSystemWatcher(path, filter) { EnableRaisingEvents = true };

                    var sources = new[] 
                    { 
                        Observable.FromEventPattern<FileSystemEventArgs>(fileSystemWatcher, "Created")
                            .Select(ev => new FileChangedEvent()),
 
                        Observable.FromEventPattern<FileSystemEventArgs>(fileSystemWatcher, "Changed")
                            .Select(ev => new FileChangedEvent()),
 
                        Observable.FromEventPattern<RenamedEventArgs>(fileSystemWatcher, "Renamed")
                            .Select(ev => new FileChangedEvent()),
 
                        Observable.FromEventPattern<FileSystemEventArgs>(fileSystemWatcher, "Deleted")
                            .Select(ev => new FileChangedEvent()),
 
                        Observable.FromEventPattern<ErrorEventArgs>(fileSystemWatcher, "Error")
                            .SelectMany(ev => Observable.Throw<FileChangedEvent>(ev.EventArgs.GetException()))
                    };

                    return sources.Merge()
                        .Throttle(throttle)
                        .Finally(() => fileSystemWatcher.Dispose())
                        .Subscribe(observer);
                }
                );
        }
    }*/
}