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

    public static string TexAssembleLocation
    {
        get => _settingsFile.GetValue("Settings", "TexAssemblePath", "").AsString();
        set
        {
            _settingsFile.SetValue("Settings", "TexAssemblePath", value);
            _dirty = true;
        }
    }

    public static string TexConvLocation
    {
        get => _settingsFile.GetValue("Settings", "TexConvPath", "").AsString();
        set
        {
            _settingsFile.SetValue("Settings", "TexConvPath", value);
            _dirty = true;
        }
    }
    
    public static float MinBrightness
    {
        get => _settingsFile.GetValue("Settings", "MinBrightness", 0.000f).AsSingle();
        set
        {
            _settingsFile.SetValue("Settings", "MinBrightness", value);
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

    private static bool _dirty; // Marks that it's time to save settings

    public override void _Ready()
    {
        _settingsFile = new ConfigFile();
        
        if (_settingsFile.Load(SettingsPath) != Error.Ok)
        {
            SetDefaults();
            SaveSettings();
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
