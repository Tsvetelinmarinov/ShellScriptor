using System.ComponentModel;

namespace ShellScriptor.Services;

[Description("Service for the font size value. Exists to prevent the program entering endless loops because" +
             " of the complex actions of ReactiveObject class")]
public class FontSizeService
{
    public double FontSize { get; set; } = 20;
}