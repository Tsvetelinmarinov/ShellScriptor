using System;
using System.IO;
using System.Text.Json;
using Avalonia.Media;
using AvaloniaEdit.Document;
using ReactiveUI;
using ShellScriptor.Services;

namespace ShellScriptor.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    #region Properties
    
    //Default dark mode background color.
    public static IBrush Background => Brush.Parse("#FF1A191C");
    
    // This property makes the connection between MainWindow.TextEditor and this class(model).
    public TextDocument? Document { get; set; } = new();

    public double FontSizе
    {
        get;
        set
        {
            this.RaiseAndSetIfChanged(ref field, value);
            this.ExportFontSize();
        }
    }

    #endregion


    public MainWindowViewModel()
    {
        this.FontSizе = LoadFontSize().FontSize;
    }
    
    
    // Exports the font size value to a JSON file.
    private void ExportFontSize()
    {
        const string jsonName = ".SS_font_size_config";
        const string appDir = "ShellScriptor";
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        if (!Directory.Exists(Path.Combine(appData, appDir)))
        {
            Directory.CreateDirectory(Path.Combine(appData, appDir));
        }

        var json = JsonSerializer.Serialize(new FontSizeService { FontSize = this.FontSizе });
        File.WriteAllText(
            Path.Combine(appData, appDir, jsonName),
            json
        );
    }
    
    // Loads the font size value from a JSON file
    private static FontSizeService LoadFontSize()
    {
        const string jsonName = ".SS_font_size_config";
        const string appDir = "ShellScriptor";
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        if (!File.Exists(Path.Combine(appData, appDir, jsonName)))
        {
            return new FontSizeService();
        }

        var json = File.ReadAllText(Path.Combine(appData, appDir, jsonName));
        
        return JsonSerializer
                   .Deserialize<FontSizeService>(json)
                    ?? new FontSizeService();
    }
}