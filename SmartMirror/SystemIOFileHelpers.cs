using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace SmartMirror
{
    public static class SystemIOFileHelpers
    {
        public static T EnrichIoException<T>(string path, Func<T> func)
        {
            try
            {
                return func();
            }
            catch (EnrichedIOException)
            {
                throw;
            }
            catch (IOException ioe)
            {
                throw ioe.AddPath(path);
            }
        }

        public static void EnrichIoException(string path, Action func)
        {
            try
            {
                func();
            }
            catch (EnrichedIOException)
            {
                throw;
            }
            catch (IOException ioe)
            {
                throw ioe.AddPath(path);
            }
        }

        public static T EnrichIoException<T>(string path, string path2, Func<T> func)
        {
            try
            {
                return func();
            }
            catch (EnrichedIOException)
            {
                throw;
            }
            catch (IOException ioe)
            {
                throw ioe.AddPaths(path, path2);
            }
        }

        public static void EnrichIoException(string path, string path2, Action func)
        {
            try
            {
                func();
            }
            catch (EnrichedIOException)
            {
                throw;
            }
            catch (IOException ioe)
            {
                throw ioe.AddPaths(path, path2);
            }
        }

        public static T Persist<T>(Func<T> func, int maxTries = 5)
        {
            var tries = 0;
            while (true)
            {
                try
                {
                    return func();
                }
                catch (IOException)
                {
                    if (++tries >= maxTries)
                        throw;
                    FileIORetrySleep();
                }
            }
        }

        private static void FileIORetrySleep()
        {
            System.Threading.Thread.Sleep(25);
        }

        public static void Persist(Action func, int maxTries = 5)
        {
            var tries = 0;
            while (true)
            {
                try
                {
                    func();
                    return;
                }
                catch (IOException)
                {
                    if (++tries >= maxTries)
                        throw;
                    FileIORetrySleep();
                }
            }
        }

        public static void AppendAllText_Persist(this string path, string contents)
        {
            Persist(() => path.AppendAllText(contents));
        }

        public static void AppendAllText(this string path, string contents)
        {
            System.IO.File.AppendAllText(path, contents);
        }

        public static void AppendLine_Persist(this string filepath, string line)
        {
            Persist(() => filepath.AppendLine(line));
        }

        public static void AppendLine(this string filepath, string line)
        {
            filepath.AppendAllLines(new[] { line });
        }

        public static void AppendAllLines_Persist(this string filepath, string[] lines)
        {
            Persist(() => filepath.AppendAllLines(lines));
        }

        public static void AppendAllLines(this string filepath, string[] lines)
        {
            EnrichIoException(filepath, () => System.IO.File.AppendAllLines(filepath, lines));
        }


        public static bool FileExists(this string filePath)
        {
            return EnrichIoException(filePath,
                ()

                    =>
                {
                    if (System.IO.File.Exists(filePath))
                        return true;
                    try
                    {
                        return new FileInfo(filePath).Length >= 0;
                    }
                    catch (FileNotFoundException)
                    {
                        return false;
                    }
                }

                );
        }

        public static bool FileExists0(this string filePath)
        {
            return EnrichIoException(filePath,
                () =>
                {
                    if (System.IO.File.Exists(filePath))
                        return true;

                    // If f.e says no, we should check whether it's because it can't tell, or because it can't determine

                    try
                    {
                        var files =
                            Directory.GetFiles(
                                filePath.GetDirectoryName(), filePath.GetFileName(),
                                SearchOption.TopDirectoryOnly);
                        return files.Length > 0;
                    }
                    catch (DirectoryNotFoundException)
                    {
                        return false;
                    }
                });
        }

        public static byte[] ReadAllBytes(this string filePath)
        {
            return EnrichIoException(filePath, () => System.IO.File.ReadAllBytes(filePath));
        }

        public static string ReadAllText(this string filePath)
        {
            return EnrichIoException(filePath, () => System.IO.File.ReadAllText(filePath));
        }

        public static string ReadAllText_Persist(this string filePath)
        {
            return Persist(() => filePath.ReadAllText());
        }

        public static string[] ReadAllLines(this string filePath)
        {
            return EnrichIoException(filePath, () => System.IO.File.ReadAllLines(filePath));
        }

        public static string[] ReadAllLines_Persist(this string filePath)
        {
            return Persist(() => filePath.ReadAllLines());
        }

        private static void WriteAllText(this string filePath, string text)
        {
            EnrichIoException(filePath,
                () =>
                {
                    filePath.CreateDirectoryForFile();
                    System.IO.File.WriteAllText(filePath, text);
                });
        }

        public static void WriteAllText_Persist(this string filePath, string text)
        {
            Persist(() => filePath.WriteAllText(text));
        }

        private static void WriteAllLines(this string filePath, IEnumerable<string> lines)
        {
            EnrichIoException(filePath,
                () =>
                {
                    filePath.CreateDirectoryForFile();
                    System.IO.File.WriteAllLines(filePath, lines.ToArray());
                });
        }

        public static void WriteAllLines_Persist(this string filePath, IEnumerable<string> lines)
        {
            Persist(() => filePath.WriteAllLines(lines));
        }

        private static void WriteAllBytes(this string filePath, byte[] bytes)
        {
            EnrichIoException(filePath,
                () =>
                {
                    filePath.CreateDirectoryForFile();
                    System.IO.File.WriteAllBytes(filePath, bytes);
                });
        }

        public static void WriteAllBytes_Persist(this string filePath, byte[] bytes)
        {
            Persist(() => filePath.WriteAllBytes(bytes));
        }



        public static void MoveFile(this string filePath, string targetPath)
        {
            EnrichIoException(filePath, targetPath,
                () =>
                {
                    targetPath.GetDirectoryName().CreateDirectory();
                    System.IO.File.Move(filePath, targetPath);
                });
            targetPath.AssertExists_Persist();
        }

        public static void MoveFile_Persist(this string filePath, string targetPath)
        {
            Persist(() => filePath.MoveFile(targetPath));
        }

        [DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern bool CreateHardLink(
            string lpFileName,
            string lpExistingFileName,
            IntPtr lpSecurityAttributes
            );

        public static bool TryHardLinkFile(this string filePath, string targetPath)
        {
            try
            {
                targetPath.GetDirectoryName().CreateDirectory();
                if (!CreateHardLink(targetPath, filePath, IntPtr.Zero))
                    return false;
            }
            catch
            {
                return false;
            }
            targetPath.AssertExists_Persist();
            return true;
        }

        public static void AssertExists(this string filePath)
        {
            if (!filePath.FileExists())
                throw new EnrichedIOException(string.Format("File '{0}' should exist, but doesn't", filePath), null);
        }

        public static void AssertExists_Persist(this string filePath)
        {
            Persist(() => filePath.AssertExists(), 1000);
        }

        public static void CopyFile(this string filePath, string targetPath)
        {
            EnrichIoException(filePath, targetPath,
                () =>
                {
                    targetPath.GetDirectoryName().CreateDirectory();
                    if (!CreateHardLink(targetPath, filePath, IntPtr.Zero))
                        System.IO.File.Copy(filePath, targetPath, true);
                });
            targetPath.AssertExists_Persist();
        }

        public static void CopyFile_Persist(this string filePath, string targetPath)
        {
            Persist(() => filePath.CopyFile(targetPath));
        }

        public static void DeleteFile(this string filePath)
        {
            EnrichIoException(filePath,
                () =>
                {
                    if (!filePath.FileExists())
                        return;
                    try
                    {
                        // First try deleting immediately; theoretically I do have the rights to delete but not to set attributes
                        System.IO.File.Delete(filePath);
                    }
                    catch (IOException)
                    {
                        // Clear Read Only / System flag
                        System.IO.File.SetAttributes(filePath, FileAttributes.Normal);
                        System.IO.File.Delete(filePath);
                    }
                });
        }

        public static void DeleteFile_Persist(this string filePath)
        {
            Persist(() => filePath.DeleteFile());
        }

        public static Int64 FileSize(this string filePath)
        {
            return EnrichIoException(filePath, () => new FileInfo(filePath).Length);
        }

        public static DateTime FileCreationTime(this string filePath)
        {
            return EnrichIoException(filePath, () => new FileInfo(filePath).CreationTimeUtc);
        }
    }
}