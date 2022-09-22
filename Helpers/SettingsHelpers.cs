using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;

namespace QuickFixMyPic.Helpers;

public static class SettingsHelpers
{
    public static GeneralSettings LoadGeneralSettingsFromDisk()
    {
        string fileName = "generalsettings.dat";

        var settings = GetFileContents(fileName, false);
        GeneralSettings mySettings = new GeneralSettings();
        if(string.IsNullOrEmpty(settings))
        {
            mySettings = new GeneralSettings()
            {
            };
            var serialGS = JsonSerializer.Serialize<GeneralSettings>(mySettings);
            WriteFile(fileName, serialGS, false);
        }
        else
        {
            mySettings = JsonSerializer.Deserialize<GeneralSettings>(settings) ?? new GeneralSettings() { };
        }
        return mySettings;
    }

    public static void SaveGeneralSettingsToDisk(GeneralSettings settings)
    {
        string fileName = "generalsettings.dat";
        var serialA = JsonSerializer.Serialize<GeneralSettings>(settings);
        WriteFile(fileName, serialA, false);
    }

    static string GetFileContents(string fileName, bool isEncrypted)
    {
        string basePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\ImageConverter\\";
        if(!File.Exists(basePath + fileName))
        {
            return "";
        }
        else
        {
            var fileText = File.ReadAllText(basePath + fileName);
            return fileText;
        }
    }

    static void WriteFile(string filename, string contents, bool isEncrypted)
    {
        string basePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\ImageConverter\\";
        if(!Directory.Exists(basePath))
        {
            Directory.CreateDirectory(basePath);
        }
        File.WriteAllText(basePath + filename, contents);
    }
}

public class GeneralSettings
{
}