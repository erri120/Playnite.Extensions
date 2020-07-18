using Playnite.SDK;
using System.Collections.Generic;

namespace VNDBMetadata
{
    public static class StaticSettings
    {
        public static bool UseTLS { get; set; } = true;
        public static int MaxTags { get; set; } = 50;
        public static int TagsCacheLivetime { get; set; } = 1;
    }

    public class VNDBMetadataSettings : ISettings
    {
        private readonly VNDBMetadata _plugin;

        public bool UseTLS
        {
            get => StaticSettings.UseTLS;
            set => StaticSettings.UseTLS = value;
        }

        public int MaxTags
        {
            get => StaticSettings.MaxTags;
            set => StaticSettings.MaxTags = value;
        }

        public int TagsCacheLivetime
        {
            get => StaticSettings.TagsCacheLivetime;
            set => StaticSettings.TagsCacheLivetime = value;
        }

        // Parameterless constructor must exist if you want to use LoadPluginSettings method.
        public VNDBMetadataSettings()
        {
        }

        public VNDBMetadataSettings(VNDBMetadata plugin)
        {
            // Injecting your plugin instance is required for Save/Load method because Playnite saves data to a location based on what plugin requested the operation.
            _plugin = plugin;

            // Load saved settings.
            var savedSettings = plugin.LoadPluginSettings<VNDBMetadataSettings>();

            // LoadPluginSettings returns null if not saved data is available.
            if (savedSettings != null)
            {
                UseTLS = savedSettings.UseTLS;
                MaxTags = savedSettings.MaxTags;
                TagsCacheLivetime = savedSettings.TagsCacheLivetime;
            }
        }

        public void BeginEdit()
        {
            // Code executed when settings view is opened and user starts editing values.
        }

        public void CancelEdit()
        {
            // Code executed when user decides to cancel any changes made since BeginEdit was called.
            // This method should revert any changes made to Option1 and Option2.
        }

        public void EndEdit()
        {
            // Code executed when user decides to confirm changes made since BeginEdit was called.
            // This method should save settings made to Option1 and Option2.
            _plugin.SavePluginSettings(this);
        }

        public bool VerifySettings(out List<string> errors)
        {
            // Code execute when user decides to confirm changes made since BeginEdit was called.
            // Executed before EndEdit is called and EndEdit is not called if false is returned.
            // List of errors is presented to user if verification fails.
            errors = new List<string>();
            return true;
        }
    }
}