using ImageMagick;
using Microsoft.Win32;
using QuickFixMyPic.Helpers;
using QuickFixMyPic.Properties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.DirectoryServices.ActiveDirectory;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using Wpf.Ui.Mvvm.Contracts;
using Wpf.Ui.Mvvm.Services;
using static System.Net.Mime.MediaTypeNames;

namespace QuickFixMyPic;

public partial class MainWindow : UiWindow
{
    public ObservableCollection<string> FilesToConvert { get; set; }
    public List<string> ConvertToTypesAvailable { get; set; }
    public string SelectedConvertToType { get; set; }

    NamedPipeManager PipeManager;

    public MainWindow()
    {
        FilesToConvert = new ObservableCollection<string>();
        PipeManager = new NamedPipeManager("ImageConverter");
        PipeManager.StartServer();
        PipeManager.ReceiveString += HandleNamedPipe_OpenRequest;
        ConvertToTypesAvailable = new List<string>() { "No Conversion", "JPG", "PNG", "GIF", "WEBP", "HEIC", "BMP", "TIFF", "ICO" };
        this.DataContext = this;
        InitializeComponent();

        var args = Environment.GetCommandLineArgs();
        string putThemBackTogether = string.Join(' ', args);
        var runningDllFullPathToRemove = System.Reflection.Assembly.GetEntryAssembly().Location;
        var onlyTheParam = putThemBackTogether.Replace(runningDllFullPathToRemove, "");
        if(!string.IsNullOrEmpty(onlyTheParam))
        {
            FilesToConvert.Add(onlyTheParam.Trim());
        }
    }


    public void HandleNamedPipe_OpenRequest(string fileToOpen)
    {
        Dispatcher.Invoke(() =>
        {
            if(!string.IsNullOrEmpty(fileToOpen))
            {
                var extension = System.IO.Path.GetExtension(fileToOpen);
                if(extension?.ToLower() != ".dll")
                {
                    FilesToConvert.Add(fileToOpen.Trim());
                }
            }

            if(WindowState == WindowState.Minimized)
            {
                WindowState = WindowState.Normal;
            }

            this.Topmost = true;
            this.Activate();
            Dispatcher.BeginInvoke(new Action(() =>
            {
                this.Topmost = false;
            }));
        });
    }

    private void EngageClicked(object sender, RoutedEventArgs e)
    {
        var finalType = SelectedConvertToType;
        if(FilesToConvert.Count == 0)
        {
            RootSnackbar.Show("Whoops", "No files selected", Wpf.Ui.Common.SymbolRegular.Warning16);
            return;
        }
        var deleteOriginals = chkDeleteOrig.IsChecked;
        var targetFiles = FilesToConvert.ToList();
        var fixWidth = 0;
        var fixHeight = 0;
        var doConvert = true;
        var doResize = chkResize.IsChecked ?? false;
        int.TryParse(txtWidth.Text, out fixWidth);
        int.TryParse(txtHeight.Text, out fixHeight);
        if(fixWidth == 0 || fixHeight == 0)
        {
            doResize = false;
        }
        pbMainProgress.Value = 0;
        MagickFormat targetFormat = MagickFormat.Jpg;
        switch(finalType)
        {
            case "JPG":
            {
                targetFormat = MagickFormat.Jpg;
            }
                break;
            case "PNG":
            {
                targetFormat = MagickFormat.Png;
            }
                break;
            case "GIF":
            {
                targetFormat = MagickFormat.Gif;
            }
                break;
            case "WEBP":
            {
                targetFormat = MagickFormat.WebP;
            }
                break;
            case "BMP":
            {
                targetFormat = MagickFormat.Bmp;
            }
                break;
            case "TIFF":
            {
                targetFormat = MagickFormat.Tiff;
            }
                break;
            case "ICO":
            {
                targetFormat = MagickFormat.Ico;
            }
                break;
            default:
            {
                doConvert = false;
            }
                break;
        }

        if(doConvert == false && doResize == false)
        {
            RootSnackbar.Show("Whoops", "No operations selected", Wpf.Ui.Common.SymbolRegular.Warning16);
            return;
        }
        chkDeleteOrig.IsEnabled = false;
        btnEngage.IsEnabled = false;
        lbTargetFiles.IsEnabled = false;
        txtHeight.IsEnabled = false;
        txtWidth.IsEnabled = false;
        chkResize.IsEnabled = false;

        Progress<double> prog = new Progress<double>();
        prog.ProgressChanged += (a, b) =>
        {
            pbMainProgress.Value = b;
            Wpf.Ui.TaskBar.TaskBarProgress.SetValue(this, Wpf.Ui.TaskBar.TaskBarProgressState.Normal, (int)(b * 100));
        };
        var counter = 0;
        Task.Factory.StartNew(() =>
        {
            try
            {
                IProgress<double> pro = prog;
                foreach(var item in targetFiles)
                {
                    using(var image = new MagickImage(item))
                    {
                        var fileName = System.IO.Path.GetFileName(item);
                        var extension = System.IO.Path.GetExtension(item);//includes the .
                        var rootPath = item.Replace(fileName, "");
                        string fullWriteFilePath = item;
                        if(deleteOriginals == true)// delete early so we can use the same filename 
                        {
                            File.Delete(item);
                        }
                        if(doConvert)
                        {
                            image.Format = targetFormat;
                            if(targetFormat == MagickFormat.Ico)
                            {
                                image.Settings.SetDefine("icon:auto-resize", "256,128,96,64,48,32,16");
                            }
                            fullWriteFilePath = System.IO.Path.Combine(rootPath, fileName.Replace(extension, "." + finalType.ToLower()));
                        }
                        fullWriteFilePath = GeneralHelpers.CheckPathForDupesAndIncIfNeeded(fullWriteFilePath);
                        if(doResize && fixHeight > 0 && fixWidth > 0)
                        {
                            image.Resize(fixWidth, fixHeight);
                        }
                        image.Write(fullWriteFilePath);
                        lbTargetFiles.Dispatcher.Invoke(() =>
                        {
                            FilesToConvert.Remove(item);
                        });
                        counter++;
                        pro.Report((double)counter / (double)targetFiles.Count);
                    }
                }
            }
            catch(Exception ex)
            {
                System.Windows.MessageBox.Show(ex.ToString(), "Error");
            }
        }).ContinueWith(a =>
        {
            RootSnackbar.Show("Victory!", "All Targets Down", Wpf.Ui.Common.SymbolRegular.EmojiSmileSlight20);
            txtHeight.IsEnabled = true;
            txtWidth.IsEnabled = true;
            chkResize.IsEnabled = true;
            chkDeleteOrig.IsEnabled = true;
            btnEngage.IsEnabled = true;
            lbTargetFiles.IsEnabled = true;
            pbMainProgress.Value = 0;
            Wpf.Ui.TaskBar.TaskBarProgress.SetValue(this, Wpf.Ui.TaskBar.TaskBarProgressState.Normal, 0);
        }, TaskScheduler.FromCurrentSynchronizationContext());
    }

    private void ListBox_Drop(object sender, DragEventArgs e)
    {
        if(e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if(files == null || files.Length == 0)
            {
                return;
            }
            foreach(var item in files)
            {
                if(string.IsNullOrEmpty(item) || FilesToConvert.Contains(item))
                {
                    continue;
                }
                FilesToConvert.Add(item);
            }
        }
    }

    private void UiWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        PipeManager.StopServer();
    }

    ~MainWindow()
    {
        PipeManager.StopServer();
    }

    private void btnBrowseFiles_Click(object sender, RoutedEventArgs e)
    {
        var fileBrowser = new Microsoft.Win32.OpenFileDialog() { CheckFileExists = true, Multiselect = true, };
        fileBrowser.ShowDialog();
        if(fileBrowser.FileNames != null && fileBrowser.FileNames.Length > 0)
        {
            foreach(string item in fileBrowser.FileNames)
            {
                FilesToConvert.Add(item.Trim());
            }
        }
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Environment.Exit(0);
    }

    private void FolderBrowse_Click(object sender, RoutedEventArgs e)
    {
        var selectedFolder = new System.Windows.Forms.FolderBrowserDialog();
        var userFolderDialog = selectedFolder.ShowDialog();
        if(userFolderDialog == System.Windows.Forms.DialogResult.OK)
        {
            var folderFiles = Directory.EnumerateFiles(selectedFolder.SelectedPath, "*.*", SearchOption.AllDirectories);
            foreach(var file in folderFiles)
            {
                FilesToConvert.Add(file.Trim());
            }
        }
    }

    private async void About_Click(object sender, RoutedEventArgs e)
    {
        About about = new About();
        about.ShowDialog();
    }

    private void Help_Click(object sender, RoutedEventArgs e)
    {
        var myProcess = new System.Diagnostics.Process();
        myProcess.StartInfo.UseShellExecute = true;
        myProcess.StartInfo.FileName = "https://github.com/Echostorm44/QuickFixMyPic/wiki";
        myProcess.Start();
    }

    private async void CheckForUpdates_Click(object sender, RoutedEventArgs e)
    {
        string resultText = "Your version is up to date";
        bool hasNewVersion = false;
        Wpf.Ui.TaskBar.TaskBarProgress.SetValue(this, Wpf.Ui.TaskBar.TaskBarProgressState.Indeterminate, 80);
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version.ToString();
        using(HttpClient client = new HttpClient())
        {
            var result = await client.GetAsync("https://raw.githubusercontent.com/Echostorm44/QuickFixMyPic/main/version.txt");
            if(result.IsSuccessStatusCode)
            {
                var availableVersion = await result.Content.ReadAsStringAsync();
                if(availableVersion != version)
                {
                    resultText = "A newer version is available.";
                    hasNewVersion = true;
                }
            }
        }
        Wpf.Ui.TaskBar.TaskBarProgress.SetValue(this, Wpf.Ui.TaskBar.TaskBarProgressState.None, 0);
        var mb = new Wpf.Ui.Controls.MessageBox();
        mb.ButtonLeftAppearance = Wpf.Ui.Common.ControlAppearance.Secondary;
        mb.ButtonLeftName = "Close";
        mb.ButtonRightName = "OK";
        mb.ButtonLeftClick += CloseUpdateMessageBoxClick;
        mb.ButtonRightClick += CloseUpdateMessageBoxClick;
        if(hasNewVersion)
        {
            mb.ButtonRightClick += UpdateMessageBoxRightButtonClick;
            mb.ButtonRightName = "Let's go get it!";
        }
        mb.Show("Update Check", resultText);
    }

    private void UpdateMessageBoxRightButtonClick(object sender, RoutedEventArgs e)
    {
        var myProcess = new System.Diagnostics.Process();
        myProcess.StartInfo.UseShellExecute = true;
        myProcess.StartInfo.FileName = "https://github.com/Echostorm44/QuickFixMyPic/releases";
        myProcess.Start();
        (sender as Wpf.Ui.Controls.MessageBox)?.Close();
    }

    private void CloseUpdateMessageBoxClick(object sender, RoutedEventArgs e)
    {
        (sender as Wpf.Ui.Controls.MessageBox)?.Close();
    }

    private void ClearSelected_Click(object sender, RoutedEventArgs e)
    {
        FilesToConvert.Clear();
    }

    //var pathToThisEXE = Environment.ProcessPath;
    //RegistryKey _key = Registry.ClassesRoot.OpenSubKey($"*\\shell", true);
    //RegistryKey newkey = _key.CreateSubKey("Convert Images Sucka");
    //newkey.SetValue("AppliesTo", ".png OR .jpg OR .gif OR .tiff OR .webp OR .heic OR .jpeg OR .bmp");
    //RegistryKey subNewkey = newkey.CreateSubKey("Command");
    //subNewkey.SetValue("", pathToThisEXE + " %1");
    //subNewkey.Close();
    //newkey.Close();
    //_key.Close();
}
