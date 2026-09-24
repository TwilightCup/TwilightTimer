namespace TwilightTimer
{
    /// <summary>
    /// Top-level service that owns all the user-editable models and wires
    /// hot-reload / save. Created once at plugin boot; subsystems read from the
    /// instance properties. Keeps concerns off the engine MonoBehaviour.
    /// </summary>
    public sealed class ConfigService
    {
        public readonly SettingsModel Settings = new SettingsModel();
        public readonly EnabledTagsModel EnabledTags = new EnabledTagsModel();
        public readonly LayoutModel Layout = new LayoutModel();
        public readonly LocalizationService Localization = new LocalizationService();

        public static ConfigService Instance { get; private set; }

        /// <summary>
        /// Raised after <see cref="SaveSettings"/> has written every config
        /// file. TwilightTimer subscribes the settings-tab registry to this event so
        /// external tabs can persist their own config (R9.3); external plugins
        /// should subscribe to <c>SettingsPanelTabRegistry.SettingsSaved</c>
        /// instead of this internal event.
        /// </summary>
        internal event System.Action SettingsSaved;

        /// <summary>
        /// The directory every config file is read from / written to. Normally the
        /// fork's own <c>config/TwilightTimer/</c>; when the HSRTimer directory
        /// switch is on it is <c>config/HSRTimer/</c> (see
        /// <see cref="SetUseHsrtimerConfig"/>).
        /// </summary>
        public string ConfigDir => PersistenceService.PluginDir;

        /// <summary>True while the HSRTimer config directory is the active config source.</summary>
        public bool UseHsrtimerConfig => PersistenceService.UseHsrtimer;

        /// <summary>Whether an HSRTimer config directory exists to switch to.</summary>
        public bool HsrtimerConfigDirExists => PersistenceService.HsrtimerDirExists;

        /// <summary>Load everything from disk; called once at boot.</summary>
        public void Load()
        {
            // Resolve the active config directory before anything is read (the
            // switch file lives in the fork's own dir, so this is unambiguous).
            PersistenceService.LoadDirectorySwitch();
            PersistenceService.EnsureDirs();
            Settings.Load();
            EnabledTags.Load();
            Layout.Load();
            Localization.Reload();
            Localization.SetLanguage(Settings.CurrentLang);
        }

        /// <summary>
        /// Switch the whole config source between the fork's own directory and the
        /// upstream HSRTimer directory, applied live. Writes the switch, drops every
        /// cached model, re-reads the target directory, repairs it, and re-arms the
        /// preset store so a missing target config gets the normal defaults. Not
        /// allowed while a match is active (it would overwrite the pushed round tag
        /// set, T5.3) — callers gate on <c>!MatchMode.Active</c>.
        /// </summary>
        public void SetUseHsrtimerConfig(bool useHsrtimer)
        {
            if (MatchMode.Active)
            {
                if (Plugin.Logger != null)
                    Plugin.Logger.LogWarning("TwilightTimer: config directory switch refused during a match round.");
                return;
            }
            if (PersistenceService.UseHsrtimer == useHsrtimer)
                return;

            PersistenceService.SaveDirectorySwitch(useHsrtimer);
            PersistenceService.EnsureDirs();

            // Re-read everything from the newly selected directory.
            ReloadAll();
            ConfigRepair.Run(this);
            PresetStore.EnsureInitialized(this);

            SettingsPanelTabRegistry.Instance?.NotifyLanguageChanged(Localization.CurrentCode);

            // Runtime caches that carry config-derived paths must drop the old
            // directory's files; both re-read lazily from the new one.
            MarkersManager.Instance?.InvalidateAll();
            MarkersPanel.InvalidateCache();

            if (Plugin.Logger != null)
                Plugin.Logger.LogInfo($"TwilightTimer: config source = {ConfigDir}");
        }

        /// <summary>Re-read all files from disk without restarting (hot-reload).</summary>
        public void ReloadAll()
        {
            Settings.Load();
            EnabledTags.Load();
            Layout.Load();
            Localization.Reload();
            Localization.SetLanguage(Settings.CurrentLang);
        }

        /// <summary>Persist mutable settings back to disk.</summary>
        public void SaveSettings()
        {
            Settings.CurrentLang = Localization.CurrentCode;
            // While a match is active, the in-memory tag set is the pushed
            // round set, not the user's — swap the snapshot back in so the
            // save always writes the user's own tags.ini (A1). The forced
            // pause rule (T7.3) never touches the settings field, so only
            // the tag list needs guarding.
            MatchMode.PushUserValuesForSave();
            try
            {
                Settings.Save();
                EnabledTags.Save();
                Layout.Save();
            }
            finally
            {
                MatchMode.RestoreMatchOverrides();
            }

            var saved = SettingsSaved;
            if (saved != null)
            {
                try { saved(); }
                catch (System.Exception ex)
                {
                    if (Plugin.Logger != null)
                        Plugin.Logger.LogWarning($"TwilightTimer: config-saved handler threw: {ex.Message}");
                }
            }
        }

        /// <summary>Re-scan language files and re-apply the current language.</summary>
        public void ReloadLanguage()
        {
            Localization.Reload();
            Localization.SetLanguage(Settings.CurrentLang);
        }

        public static void Init(ConfigService instance) => Instance = instance;
    }
}
