using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public static class Globals
    {
        static Globals()
        {
            AllUnprocessedVideoExtensions = new[] { ".AVI", ".MOV", ".MP4", ".3GP", ".3G2", ".MPG", ".MTS", ".WMV" };
            AllImageExtensions = new[] { ".JPG", ".THM", ".PNG" };
            AllVideoExtensions = AllUnprocessedVideoExtensions.Concat(new[] {".M4V"}).ToArray();
            AllExtensionsToProcess = AllVideoExtensions.Concat(AllImageExtensions).ToArray();
        }

        public static IEnumerable<string> AllUnprocessedVideoExtensions { get; private set; }
        public static IEnumerable<string> AllVideoExtensions { get; private set; }
        public static IEnumerable<string> AllImageExtensions { get; private set; }
        public static IEnumerable<string> AllExtensionsToProcess { get; private set; }
    }

    public static class Helpers
    {
        public static bool IsFileOfType(this string path, IEnumerable<string> extensions)
        {
            return extensions.Any(ext =>
            {
                var extension = Path.GetExtension(path);
                return ext.Equals(extension, StringComparison.InvariantCultureIgnoreCase);
            });
        }

        public static bool IsSupportedFile(this string path)
        {
            return IsFileOfType(path, Globals.AllExtensionsToProcess);
        }

        public static bool IsImageFile(this string path)
        {
            return IsFileOfType(path, Globals.AllImageExtensions);
        }

        public static bool IsVideoFile(this string path)
        {
            return IsFileOfType(path, Globals.AllVideoExtensions);
        }

        public static bool IsUnprocessedVideoFile(this string path)
        {
            return IsFileOfType(path, Globals.AllUnprocessedVideoExtensions);
        }
    }
}
