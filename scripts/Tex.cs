using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Godot;
using WildRP.AMVTool.Autoloads;
// ReSharper disable InconsistentNaming

namespace WildRP.AMVTool;

public class Tex
{
    private readonly Process _process;
    private string _processName;

    public Action Exited;
    
    public Tex()
    {
        _process = new Process();
        _process.EnableRaisingEvents = true;
        _process.StartInfo.RedirectStandardOutput = true;
        _process.StartInfo.RedirectStandardError = true;
        _process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
        _process.StartInfo.WorkingDirectory = SaveManager.GetGlobalizedProjectPath();
        
        _process.Exited += (sender, args) =>
        {
            var color = _process.ExitCode == 0 ? "green" : "red";

            while (_process.StandardOutput.EndOfStream == false)
            {
                GD.Print(_process.StandardOutput.ReadLine());
            }
            
            while (_process.StandardError.EndOfStream == false)
            {
                GD.Print(_process.StandardError.ReadLine());
            }
            
            GD.PrintRich($"[color={color}][i]{_processName} finished with code {_process.ExitCode}[/i][/color].\n\n");
            Exited?.Invoke();
        };
    }

    public void SetupTexAssemble(string fileList, string target, TextureFormat format, string extraFlags = "", bool run = true)
    {
        _process.StartInfo.FileName = Settings.TexAssembleLocation;
        _processName = Settings.TexAssembleLocation.GetFile();

        var args = $"array -nologo -O \"{target}\" -y {extraFlags}";

        if (format == TextureFormat.R8G8B8A8_UNORM_SRGB)
            args += " -srgb ";

        args += $"-f {Enum.GetName(format)} -flist \"{fileList}\"";

        _process.StartInfo.Arguments = args;
        
        if (run) 
            Run();
    }

    public void SetupTexConv(string file, string outputFolder, bool compress = true, int size = 0, bool srgb = false, bool run = true, string extraFlags = "")
    {
        _process.StartInfo.FileName = Settings.TexConvLocation;
        _processName = Settings.TexConvLocation.GetFile();

        var args = $"-nologo -m 1 -y -O \"{outputFolder}\" ";

        if (compress)
        {
            args += srgb ? "-f BC3_UNORM_SRGB " : "-f BC3_UNORM ";
        }
        if (srgb) args += "-srgb ";
        if (size > 0) args += $"-w {size} -h {size} ";
        args += extraFlags;
        args += $" \"{file}\"";

        _process.StartInfo.Arguments = args;

        if (run)
            Run();
    }

    public void Run()
    {
        GD.PrintRich($"[b]==== RUNNING {_processName.ToUpper()} ====[/b]");
        GD.PrintRich($"[color=green]{_processName} {_process.StartInfo.Arguments}[/color]");
        _process.Start();
    }

    public bool Wait() => _process.WaitForExit(1000);
    
    public enum TextureFormat
    {
        None,
        R8G8B8A8_UNORM_SRGB,
        R8G8B8A8_UNORM,
        R11G11B10_FLOAT,
        R16_UNORM
    }
    
    public static Tex Assemble(string fileList, string target, TextureFormat format, string extraFlags = "")
    {
        var p = new Tex();
        p.SetupTexAssemble(fileList, target, format, extraFlags, false);
        return p;
    }

    public static Tex Conv(string file, string outputFolder, bool compress = true, int size = 0, bool srgb = false, string extraFlags = "")
    {
        var p = new Tex();
        p.SetupTexConv(file, outputFolder, compress, size, srgb, false, extraFlags);
        return p;
    }
    
}