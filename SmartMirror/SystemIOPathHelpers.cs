using System;
using System.IO;

namespace SmartMirror
{
    public static class SystemIOPathHelpers
    {
        public static string GetFileName(this string filePath)
        {
            return Path.GetFileName(filePath);
        }

        public static string GetFileNameWithoutExtension(this string filePath)
        {
            return Path.GetFileNameWithoutExtension(filePath);
        }

        public static string GetDirectoryName(this string filePath)
        {
            return Path.GetDirectoryName(filePath);
        }

        public static string GetExtension(this string filePath)
        {
            return Path.GetExtension(filePath);
        }

        public static string RemoveExtension(this string filePath)
        {
            return Path.ChangeExtension(filePath, null);
        }

        public static bool IsExtension(this string filePath, string extension)
        {
            return filePath.GetExtension().Equals(extension, StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool HasExtension(this string filePath)
        {
            return Path.HasExtension(filePath);
        }

        public static string SmartCombinePath(this string emptyOrAbsoluteOrRelativePath, string rootFolder,
            Func<string> defaultRelativePath)
        {
            if (string.IsNullOrEmpty(emptyOrAbsoluteOrRelativePath))
            {
                // Empty string: keep empty, this is the way to 'disable' paths
                if (emptyOrAbsoluteOrRelativePath != null)
                    return String.Empty;

                if (string.IsNullOrEmpty(rootFolder))
                    throw new Exception("Either CIS or every Module must have RootFolder specified");
                return rootFolder.CombinePath(defaultRelativePath()).FullPath();
            }
            if (!emptyOrAbsoluteOrRelativePath.IsPathRooted())
                return rootFolder.CombinePath(emptyOrAbsoluteOrRelativePath).FullPath();
            return emptyOrAbsoluteOrRelativePath;
        }

        public static string SmartCombinePath(this string emptyOrAbsoluteOrRelativePath, string rootFolder,
            string defaultRelativePath)
        {
            return emptyOrAbsoluteOrRelativePath.SmartCombinePath(rootFolder, () => defaultRelativePath);
        }

        public static string CombinePathIfRelative(this string rootFolder, string abs_or_relative)
        {
            if (string.IsNullOrEmpty(abs_or_relative))
                return abs_or_relative;
            if (abs_or_relative.IsPathRooted())
                return abs_or_relative;
            return rootFolder.CombinePath(abs_or_relative).FullPath();
        }

        public static string Pathify(this string name)
        {
            var retVal = name;
            foreach (var invalidPathChar in Path.GetInvalidPathChars())
                retVal = retVal.Replace(invalidPathChar, '_');
            return retVal;
        }

        public static string Filenameify(this string name)
        {
            var retVal = name;
            foreach (var invalidPathChar in Path.GetInvalidFileNameChars())
                retVal = retVal.Replace(invalidPathChar, '_');
            return retVal;
        }

        public static string CompressedFilename(this string contentId)
        {
            var basePrefix = contentId.Filenameify();
            // Now, make it as verbose as possible within 32 chars
            basePrefix = basePrefix.Replace("-", "");
            basePrefix = basePrefix.Replace("_", "");
            basePrefix = basePrefix.Replace(" ", "");

            if (basePrefix.Length > 32)
                return basePrefix.Substring(0, 32);
            return basePrefix;
        }

        public static string FullPath(this string path)
        {
            return Path.GetFullPath(path);
        }

        public static string CombinePath(this string basePath, string additionToPath)
        {
            return Path.Combine(basePath, additionToPath);
        }

        public static string GetPathRoot(this string path)
        {
            return Path.GetPathRoot(path);
        }

        public static bool IsPathRooted(this string path)
        {
            return Path.IsPathRooted(path);
        }
    }
}