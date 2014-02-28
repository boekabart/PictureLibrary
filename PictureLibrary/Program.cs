using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;

namespace PictureLibrary
{
    class Program
    {
        static void Main(string[] args)
        {
        }

        static void ProcessFile(string fullPath)
        {
            if (fullPath.IsVideoFile())
                ProcessVideo(fullPath);
            else if (IsVideoThumbnail(fullPath))
                return;
            else if (fullPath.IsImageFile())
                ProcessImage(fullPath);
        }

        private static bool IsVideoThumbnail(string fullPath)
        {
            throw new NotImplementedException();
        }
    }
}
