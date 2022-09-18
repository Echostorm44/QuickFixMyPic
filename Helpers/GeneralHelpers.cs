using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickFixMyPic.Helpers;

public static class GeneralHelpers
{
    public static string CheckPathForDupesAndIncIfNeeded(string startPath)
    {
        int count = 0;
        string workPath = startPath;
        while(File.Exists(workPath))
        {
            workPath = startPath.Insert(startPath.LastIndexOf('.'), "[" + count.ToString() + "]");
            count++;
        }
        return workPath;
    }
}
