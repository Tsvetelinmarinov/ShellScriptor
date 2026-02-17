using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Highlighting.Xshd;
using ShellScriptor.ViewModels;

namespace ShellScriptor.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        this.LoadSyntaxHighlighting();
    }
    
    
    #region Functionality
   
    // Loads a file from the file system of the machine.
    public async void LoadFile(object? sender, RoutedEventArgs args)
    {
        try
        {
            var dialog = TopLevel.GetTopLevel(this)!.StorageProvider;

            var files = await dialog.OpenFilePickerAsync(
                new FilePickerOpenOptions
                {
                    AllowMultiple = false,
                    FileTypeFilter =
                    [
                        new FilePickerFileType("All files")
                        {
                            Patterns = ["*.*"]
                        }
                    ]
                }
            );

            if (files.Count <= 0) return;
        
            await using var fileStream = await files[0].OpenReadAsync();
            using var reader = new StreamReader(fileStream);
            this.Editor.Document!.Text = await reader.ReadToEndAsync();
        }
        catch (Exception e)
        {
            Debug.WriteLine($"\nError: {e.Message} at {e.StackTrace}.\n");
        }
    }
    
    // Exports the shell script in the editor to a local file.
    public async void ExportFile(object? sender, RoutedEventArgs args)
    {
        try
        {
            var fileSaver = TopLevel.GetTopLevel(this)!.StorageProvider;
            var file = await fileSaver.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "File saver",
                SuggestedFileName = "unknown_shell",
                FileTypeChoices = 
                [
                    new FilePickerFileType("All files")
                    {
                        Patterns = [ "*.*" ]
                    }
                ]
            });

            if (file != null)
            {
                await using var fileStream = await file.OpenWriteAsync();
                var writer = new StreamWriter(fileStream, Encoding.UTF8);
                await writer.WriteAsync(this.Editor.Document!.Text);
            }
        }
        catch (Exception e)
        {
            Debug.WriteLine($"\nError: {e.Message} at {e.StackTrace}.\n");
        }
    }
    
    // Executes the shell script in the editor with the default terminal of the machine.
    // Supported terminals are gnome-terminal, xfce4-terminal, xterm, kitty and alacritty
    public void Execute(object? sender, RoutedEventArgs args)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return;
        }
        
        var runningShell = Path.Combine(
            Path.GetTempPath(),
            "ShellScriptor",
            ".SS_running_shell.ssdata"
        );

        if (!Directory.Exists(Path.GetDirectoryName(runningShell)))
        {
            Directory.CreateDirectory(
                Path.Combine(
                    Path.GetTempPath(),
                    "ShellScriptor"
                )
            );
        }
        
        File.WriteAllText(runningShell, this.Editor.Document!.Text);
        
        Process.Start("chmod", $"+x {runningShell}")
            .WaitForExit();

        var terminals = new[]
        {
            new { Name = "kitty", Args = "" },
            new { Name = "alacritty", Args = "" },
            new { Name = "xterm", Args = "" },
            new { Name = "xfce4-terminal", Args = "" },
            new { Name = "konsole", Args = "" },
            new { Name = "gnome-terminal", Args = $"-- bash -c \"{runningShell}; exec bash\"" }
        };

        var availableTerm
            = terminals.FirstOrDefault(terminal => IsTerminalInstalled(terminal.Name));

        if (availableTerm == null)
        {
            Debug.WriteLine("There is no supported terminal on that machine.");
        }
        else
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = availableTerm.Name,
                Arguments = availableTerm.Args,
                UseShellExecute = true
            });
        }
    }

    // Helper function to check if the current terminal is installed on the machine
    // in the FirstOrDefault() function in the Execute() method above.
    // Note that it may return false even when the process "which" do not start.
    private static bool IsTerminalInstalled(string terminal)
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "which",
                Arguments = terminal,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true
            };

            var whichProcess = Process.Start(processInfo);
            whichProcess!.WaitForExit();
            return whichProcess.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
    
    // Opens window with option for changing the font.
    public void OpenFontSettings(object? sender, RoutedEventArgs args)
    {
        var fontWind = new FontSizeWindow
        {
            DataContext = this.DataContext
        };
        
        fontWind.Show();
    }

    private void LoadSyntaxHighlighting()
    {
        using var syntaxFileStream 
            = AssetLoader.Open(new Uri("avares://ShellScriptor/SyntaxHighlighting/shellScript.xshd"));

        using var xmlReader = XmlReader.Create(syntaxFileStream);
        
        var syntaxDef 
            = HighlightingLoader.Load(xmlReader, HighlightingManager.Instance);

        this.Editor.SyntaxHighlighting = syntaxDef;
    }
    
    #endregion 
}