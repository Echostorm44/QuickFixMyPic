using ImageMagick;
using Microsoft.Win32;
using QuickFixMyPic.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.DirectoryServices.ActiveDirectory;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
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
using Wpf.Ui.Controls;
using static System.Net.Mime.MediaTypeNames;

namespace QuickFixMyPic;

public partial class MainWindow : UiWindow
{
    public ObservableCollection<string> FilesToConvert { get; set; }
    public List<string> ConvertToTypesAvailable { get; set; }
    public string SelectedConvertToType { get; set; }

    // TODO Icon attribution <a href="https://www.flaticon.com/free-icons/image" title="image icons">Image icons created by rsetiawan - Flaticon</a>
    NamedPipeManager PipeManager;

    public MainWindow()
    {
        FilesToConvert = new ObservableCollection<string>();
        PipeManager = new NamedPipeManager("ImageConverter");
        PipeManager.StartServer();
        PipeManager.ReceiveString += HandleNamedPipe_OpenRequest;
        ConvertToTypesAvailable = new List<string>() { "JPG", "PNG", "GIF", "WEBP", "HEIC", "BMP", "TIFF", "ICO" };
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
        if(string.IsNullOrEmpty(finalType) || FilesToConvert.Count == 0)
        {
            return;
        }
        var deleteOriginals = chkDeleteOrig.IsChecked;
        var targetFiles = FilesToConvert.ToList();
        var fixWidth = 0;
        var fixHeight = 0;
        var doResize = chkResize.IsChecked ?? false;
        int.TryParse(txtWidth.Text, out fixWidth);
        int.TryParse(txtHeight.Text, out fixHeight);

        pbMainProgress.Value = 0;
        MagickFormat targetFormat = MagickFormat.Jpg;
        chkDeleteOrig.IsEnabled = false;
        btnEngage.IsEnabled = false;
        lbTargetFiles.IsEnabled = false;
        txtHeight.IsEnabled = false;
        txtWidth.IsEnabled = false;
        chkResize.IsEnabled = false;
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
        }

        Progress<double> prog = new Progress<double>();
        prog.ProgressChanged += (a, b) =>
        {
            pbMainProgress.Value = b;
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
                        image.Format = targetFormat;
                        var fileName = System.IO.Path.GetFileName(item);
                        var extension = System.IO.Path.GetExtension(item);//includes the .
                        var rootPath = item.Replace(fileName, "");
                        var fullWriteFilePath = System.IO.Path.Combine(rootPath, fileName.Replace(extension, "." + finalType.ToLower()));
                        fullWriteFilePath = GeneralHelpers.CheckPathForDupesAndIncIfNeeded(fullWriteFilePath);
                        if(doResize && fixHeight > 0 && fixWidth > 0)
                        {
                            image.Scale(fixWidth, fixHeight);
                        }
                        image.Write(fullWriteFilePath);
                        if(deleteOriginals == true)
                        {
                            File.Delete(item);
                        }
                        lbTargetFiles.Dispatcher.Invoke(() =>
                        {
                            FilesToConvert.Remove(item);
                        });
                        counter++;
                        pro.Report(((double)counter / (double)targetFiles.Count));
                    }
                }
            }
            catch(Exception ex)
            {
                System.Windows.MessageBox.Show(ex.ToString(), "Error");
            }
        }).ContinueWith(a =>
        {
            txtHeight.IsEnabled = true;
            txtWidth.IsEnabled = true;
            chkResize.IsEnabled = true;
            chkDeleteOrig.IsEnabled = true;
            btnEngage.IsEnabled = true;
            lbTargetFiles.IsEnabled = true;
            pbMainProgress.Value = 1;
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
        var goo = new System.Windows.Forms.FolderBrowserDialog();
        var userFolderDialog = goo.ShowDialog();
        if(userFolderDialog == System.Windows.Forms.DialogResult.OK)
        {
            //goo.SelectedPath;
        }
    }

    private void ToggleTheme_Click(object sender, RoutedEventArgs e)
    {
        Environment.Exit(0);
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
    }

    private void Help_Click(object sender, RoutedEventArgs e)
    {
    }

    private void CheckForUpdates_Click(object sender, RoutedEventArgs e)
    {
    }

    private void ClearSelected_Click(object sender, RoutedEventArgs e)
    {
        FilesToConvert.Clear();
    }


    //var pathToThisEXE = Environment.ProcessPath;
    //RegistryKey _key = Registry.ClassesRoot.OpenSubKey($"*\\shell", true);
    //RegistryKey newkey = _key.CreateSubKey("Convert Images Sucka");
    //newkey.SetValue("AppliesTo", ".png OR .jpg OR .gif OR .tiff OR .webp OR .heic OR .jpeg OR .bmp");
    //    RegistryKey subNewkey = newkey.CreateSubKey("Command");
    //subNewkey.SetValue("", pathToThisEXE + " %1");
    //    subNewkey.Close();
    //    newkey.Close();
    //    _key.Close();
}
