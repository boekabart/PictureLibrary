using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;

namespace SmartMirror
{
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

    public static class SystemIODirectoryHelpers
    {
        public static IEnumerable<string> SubDirectories(this string folder)
        {
            try
            {
                return Directory.GetDirectories(folder);
            }
            catch
            {
                return new string[] {};
            }
        }

        public static IEnumerable<string> SubDirectories(this string folder, string wildCard)
        {
            try
            {
                return Directory.GetDirectories(folder, wildCard);
            }
            catch
            {
                return new string[] {};
            }
        }

        public static bool DirectoryExists(this string pathToTest)
        {
            return SystemIOFileHelpers.EnrichIoException(pathToTest,
                () =>
                {
                    if (Directory.Exists(pathToTest))
                        return true;

                    // If d.e says no, we should check whether it's because it can't tell, or because it can't determine

                    if (System.IO.File.Exists(pathToTest))
                        return false;

                    try
                    {
                        // Try to get the file listing of the dir.
                        pathToTest.DirectoryFiles("_test_");
                        return true;
                    }
                    catch (DirectoryNotFoundException)
                    {
                        return false;
                    }
                }
                );
        }

        public static IEnumerable<string> TryDirectoryFiles(this string folder, string wildCard)
        {
            try
            {
                return folder.DirectoryFiles(wildCard);
            }
            catch
            {
                return new string[] {};
            }
        }

        public static IEnumerable<string> DirectoryFiles(this string folder)
        {
            return Directory.GetFiles(folder);
        }

        public static IEnumerable<string> DirectoryFiles(this string folder, string wildCard)
        {
            return Directory.GetFiles(folder, wildCard);
        }

        public static IEnumerable<string> DirectoryFilesSearchAllDirectories(this string folder, string wildCard)
        {
            return Directory.GetFiles(folder, wildCard, SearchOption.AllDirectories);
        }

        public static IEnumerable<FileInfo> DirectoryFileInfos(this string folder, string wildCard)
        {
            try
            {
                var dirInfo = new DirectoryInfo(folder);
                return dirInfo.GetFiles(wildCard, SearchOption.TopDirectoryOnly);
            }
            catch
            {
                return new FileInfo[] {};
            }
        }

        public static void CreateDirectory(this string directory)
        {
            if (!directory.DirectoryExists())
            {
                SystemIOFileHelpers.EnrichIoException(directory,
                    () =>
                    {
                        Directory.CreateDirectory(directory);
                    });
            }
        }

        public static void CreateDirectoryForFile(this string filePath)
        {
            filePath.GetDirectoryName().CreateDirectory();
        }

        public static void DeleteDirectoryFull(this string directory)
        {
            SystemIOFileHelpers.EnrichIoException(directory,
                () =>
                {
                    if (directory.DirectoryExists())
                    {
                        Directory.Delete(directory, true);
                        if (directory.DirectoryExists())
                            throw new IOException(
                                "Directory.Delete passed, but the directory still exists")
                                .AddPath(directory);
                    }
                }
                );
        }

        public static void DeleteDirectoryFull_Persist(this string directory)
        {
            SystemIOFileHelpers.Persist(() => DeleteDirectoryFull(directory));
        }

        public static bool TryDeleteDirectory(this string dir)
        {
            try
            {
                if (!dir.DirectoryExists())
                    return false;
                Directory.Delete(dir);
                return true;
            }
            catch
            {
                // Ignore
                return false;
            }
        }

        public static void DeleteDirectoryIfEmpty(this string dir)
        {
            SystemIOFileHelpers.EnrichIoException(dir,
                () =>
                {
                    if (dir.DirectoryExists())
                    {
                        Directory.Delete(dir);
                        if (dir.DirectoryExists())
                            throw new IOException(
                                "Directory.Delete passed, but the directory still exists")
                                .AddPath(dir);
                    }
                });
        }

        public static Int64 DirectorySize(this string folderPath)
        {
            return SystemIOFileHelpers.EnrichIoException(folderPath,
                () => folderPath.DirectoryFilesSearchAllDirectories("*.*")
                    .Select(fn => SystemIOFileHelpers.FileSize(fn))
                    .Sum());
        }

        public static void CopyFileOrDirectory(this string srcPath, string targetPath)
        {
            if (srcPath.IsDirectory())
                srcPath.CopyDirectory(targetPath);
            else
                srcPath.CopyFile(targetPath);
        }

        public static void CopyDirectory(this string srcDirectoryPath, string targetDirectoryPath)
        {
            SystemIOFileHelpers.EnrichIoException(srcDirectoryPath, targetDirectoryPath,
                () =>
                {
                    targetDirectoryPath.CreateDirectory();
                    foreach (var path in Directory.GetDirectories(srcDirectoryPath))
                    {
                        var destDir = path.Replace(srcDirectoryPath, targetDirectoryPath);
                        path.CopyDirectory(destDir);
                    }

                    foreach (
                        var path in
                            srcDirectoryPath.DirectoryFiles())
                    {
                        var destPath = path.Replace(srcDirectoryPath, targetDirectoryPath);
                        path.CopyFile(destPath);
                    }
                });
        }

        public static void MoveFileOrDirectory(this string filePath, string targetPath)
        {
            SystemIOFileHelpers.EnrichIoException(filePath, targetPath,
                () =>
                {
                    targetPath.GetDirectoryName().CreateDirectory();
                    if (filePath.FileExists())
                        System.IO.File.Move(filePath, targetPath);
                    else if (filePath.DirectoryExists())
                        MoveDirectory(filePath, targetPath);
                });
        }

        public static void MoveFileOrDirectory_Persist(this string filePath, string targetPath)
        {
            SystemIOFileHelpers.Persist(() => filePath.MoveFileOrDirectory(targetPath));
        }

        public static void MoveDirectory(this string sourcePath, string targetPath)
        {
            SystemIOFileHelpers.EnrichIoException(sourcePath, targetPath,
                () =>
                {
                    var sourceRoot = sourcePath.GetPathRoot();
                    var targetRoot = targetPath.GetPathRoot();
                    targetPath.GetDirectoryName().CreateDirectory();
                    if (sourceRoot.Equals(targetRoot))
                        Directory.Move(sourcePath, targetPath);
                    else
                        MoveDirectoryWithDifferentRoots(sourcePath, targetPath);
                });
        }

        private static void MoveDirectoryWithDifferentRoots(string sourcePath, string targetPath)
        {
            sourcePath.CopyDirectory(targetPath);
            sourcePath.DeleteDirectoryFull();
        }

        public static void MoveDirectory_Persist(this string filePath, string targetPath)
        {
            SystemIOFileHelpers.Persist(() => filePath.MoveDirectory(targetPath));
        }

        public static bool IsDirectory(this string path)
        {
            return SystemIOFileHelpers.EnrichIoException(path, () => path.DirectoryExists());
        }
    }
}