using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace main_app.Helper
{
    public class FileHelper
    {
        public static byte[] Convert(HttpPostedFileBase file)
        {
            MemoryStream target = new MemoryStream();
            file.InputStream.CopyTo(target);
            byte[] data = target.ToArray();
            return data;
        }
    }
}