using System;
using Godot;

// ReSharper disable MemberHidesStaticFromOuterClass

namespace WildRP.AMVTool.Autoloads;

public partial class Settings : Node
{
    private const string SettingsPath = "user://settings.cfg";
    
    // Default values
    private static class Defaults
    {
        public const float UiScale = 1f;
    }

    private static ConfigFile _settingsFile;
    public static float UiScale
    {
        get => _settingsFile.GetValue("Settings", "scale", 1f).AsSingle();
        set
        {
            _settingsFile.SetValue("Settings", "scale", value);
            _dirty = true;
        }
    }

    public static int SampleCount
    {
        get => _settingsFile.GetValue("Settings", "ProbeQuality", 10).AsInt32();
        set
        {
            _settingsFile.SetValue("Settings", "ProbeQuality", value);
            _dirty = true;
        }
    }
    
    public static int BounceCount
    {
        get => _settingsFile.GetValue("Settings", "BounceCount", 2).AsInt32();
        set
        {
            _settingsFile.SetValue("Settings", "BounceCount", value);
            _dirty = true;
        }
    }
    
    public static float BounceEnergy
    {
        get => _settingsFile.GetValue("Settings", "BounceEnergy", .33f).AsSingle();
        set
        {
            _settingsFile.SetValue("Settings", "BounceEnergy", value);
            _dirty = true;
        }
    }

    public static bool LoadMap
    {
        get => _settingsFile.GetValue("Settings", "LoadMap", true).AsBool();
        set
        {
            _settingsFile.SetValue("Settings", "LoadMap", value);
            _dirty = true;
        }
    }

    public static int BlurSize
    {
        get
        {
            var size = _settingsFile.GetValue("Settings", "BlurSize", 5).AsInt32();
            size = Mathf.Clamp(size, 3, 5);
            if (size == 4) size = 5;
            return size;
        }
        set
        {
            _settingsFile.SetValue("Settings", "BlurSize", value);
            _dirty = true;
        }
    }

    public static float BlurStrength
    {
        get => Mathf.Clamp(_settingsFile.GetValue("Settings", "BlurStrength", 0.8f).AsSingle(), 0f, 1f);
        set
        {
            _settingsFile.SetValue("Settings", "BlurStrength", value);
            _dirty = true;
        }
    }

    public static Tex.TextureFormat AmvTextureFormat
    {
        get
        {
            var value = _settingsFile.GetValue("Settings", "AmvTextureFormat", "R11G11B10_FLOAT").AsString();
            if (Enum.TryParse(value, true, out Tex.TextureFormat result) == false)
                result = Tex.TextureFormat.R11G11B10_FLOAT;
            return result;
        }
        set
        {
            _settingsFile.SetValue("Settings", "AmvTextureFormat", Enum.GetName(value));
            _dirty = true;
        }
    }

    public static string ProjectFolder
    {
        get => _settingsFile.GetValue("Settings", "ProjectFolder", "").AsString();
        set
        {
            _settingsFile.SetValue("Settings", "ProjectFolder", value);
            _dirty = true;
        }
    }

    private static bool _dirty; // Marks that it's time to save settings


    private HttpRequest _texAssembleDownloader;
    private HttpRequest _texConvDownloader;
    
    public override void _Ready()
    {
        _settingsFile = new ConfigFile();
        
        if (_settingsFile.Load(SettingsPath) != Error.Ok)
        {
            SetDefaults();
            SaveSettings();
        }
        
        if (FileAccess.FileExists("user://texassemble.exe") == false)
        {
            _texAssembleDownloader = new HttpRequest();
            AddChild(_texAssembleDownloader);
            _texAssembleDownloader.RequestCompleted += (result, code, headers, body) =>
            {
                GD.Print("Downloading texassemble:");
                GD.Print($"Code {code}");
                GD.Print(headers);
                
                using var f = FileAccess.Open("user://texassemble.exe", FileAccess.ModeFlags.Write);
                f.StoreBuffer(body);
                _texAssembleDownloader.QueueFree();
            };
            _texAssembleDownloader.Request(
                "https://github.com/Microsoft/DirectXTex/releases/latest/download/texassemble.exe");
        }
        
        if (FileAccess.FileExists("user://texconv.exe") == false)
        {
            _texConvDownloader = new HttpRequest();
            AddChild(_texConvDownloader);
            _texConvDownloader.RequestCompleted += (result, code, headers, body) =>
            {
                GD.Print("Downloading texassemble:");
                GD.Print($"Code {code}");
                GD.Print(headers);
                
                using var f = FileAccess.Open( "user://texconv.exe", FileAccess.ModeFlags.Write);
                f.StoreBuffer(body);
                _texConvDownloader.QueueFree();
            };
            _texConvDownloader.Request(
                "https://github.com/Microsoft/DirectXTex/releases/latest/download/texconv.exe");
        }
    }

    public override void _Process(double delta)
    {
        SaveSettings();
    }

    private static void SetDefaults()
    {
        UiScale = Defaults.UiScale;
    }

    private static void SaveSettings()
    {
        // Only save once a second to avoid a bunch of unecessary file writes
        if (_dirty == false || Engine.GetProcessFrames() % 60 != 0) return;

        _dirty = false;
        _settingsFile.Save(SettingsPath);
    }
}
