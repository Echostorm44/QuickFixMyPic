using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace QuickFixMyPic.Helpers;

public static class GeneralHelpers
{
    public static string CheckPathForDupesAndIncIfNeeded(string filePath)
    {
        if(File.Exists(filePath))
        {
            string folderPath = Path.GetDirectoryName(filePath);
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            string fileExtension = Path.GetExtension(filePath);
            int number = 1;

            Match regex = Regex.Match(fileName, @"^(.+)\((\d+)\)$");

            if(regex.Success)
            {
                fileName = regex.Groups[1].Value;
                number = int.Parse(regex.Groups[2].Value);
            }

            do
            {
                number++;
                string newFileName = $"{fileName}({number}){fileExtension}";
                filePath = Path.Combine(folderPath, newFileName);
            }
            while (File.Exists(filePath));
        }

        return filePath;
    }
}
