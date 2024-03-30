using Godot;
using System;
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
        get => _settingsFile.GetValue("UI", "scale", 1f).AsSingle();
        set
        {
            _settingsFile.SetValue("UI", "scale", value);
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
        if (_dirty == false) return;

        _dirty = false;
        _settingsFile.Save(SettingsPath);
    }
}
