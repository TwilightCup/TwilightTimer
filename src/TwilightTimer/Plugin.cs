using BepInEx;
using BepInEx.Logging;
using UnityEngine;

namespace TwilightTimer
{
    /// <summary>
    /// BepInEx plugin entry point. Wires every subsystem: loads config, registers
    /// the built-in tag rules, applies the voiceline Harmony patches, initializes
    /// the TwilightCore built-in LevelCollections integration, and spawns the
    /// engine + HUD singletons. TwilightCore is a hard dependency (compile-time
    /// reference + BepInDependency) — this TwilightTimer branch does not work
    /// without it.
    /// </summary>
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency("TwilightCore", BepInEx.BepInDependency.DependencyFlags.HardDependency)]
    public class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger;

        private void Awake()
        {
            Logger = base.Logger;
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} v{PluginInfo.PLUGIN_VERSION} is loaded!");

            // 1. Config + localization. Seed the runtime lang dir first (so any
            //    on-disk defaults are picked up by the scan), then load. Note:
            //    the English base + example translation are also embedded in the
            //    DLL, so localization works even if the lang/ folder is absent.
            var config = new ConfigService();
            ConfigService.Init(config);
            EnsureDefaultLangFiles();
            config.Load();
            // Detect & fill in missing/incorrect config items before any
            // subsystem reads them (e.g. insert a newly-defaulted HUD row into
            // an existing layout.ini). Idempotent; writes only on change.
            ConfigRepair.Run(config);

            // 2. Register built-in tag rules (R3.7 extension point).
            var registry = new TagRuleRegistry();
            TagRuleRegistry.Init(registry);
            registry.Register(new CheckpointTagRule());
            registry.Register(new NoCheckpointTagRule());
            registry.Register(new JumplessTagRule());
            registry.Register(new VoicelineTagRule());

            // 3. Harmony patches (voiceline hooks only).
            PatchModule.Apply();

            // 4. TwilightCore built-in LevelCollections integration (direct API).
            LcIntegration.Init();
            // Subscribe lazily too: the manager singleton may not exist yet at
            // plugin Awake (the BepInDependency only orders plugin loading).
            LcIntegration.Instance.SubscribeEvents();
            TwilightCore.CollectionManager.Instance?.StartCoroutine(SubscribeWhenReady());

            // 5. Register this plugin as TwilightCore's timer provider (T1.2).
            //    TwilightCore resolves the registry lazily per round event, so
            //    registering at the end of Awake (engine/HUD spawn below) is
            //    in time for any round; mutating calls are marshaled to the
            //    main thread by the adapter itself.
            TwilightTimerProvider.Register();

            // 6. Probe TwilightCore's (future) leaderboard feed API. Absent
            //    today → the leaderboard HUD runs in local-only transition
            //    mode (see Match/LeaderboardFeed.cs / docs/LEADERBOARD_REQ.md).
            LeaderboardFeed.Init();
        }

        private void OnDestroy()
        {
            LcIntegration.Instance?.UnsubscribeEvents();
            TwilightTimerProvider.Unregister();
        }

        /// <summary>
        /// The LC manager singleton may be created slightly after this plugin's
        /// Awake despite the dependency ordering; retry the event subscription
        /// over a few frames until it appears (idempotent once subscribed).
        /// </summary>
        private System.Collections.IEnumerator SubscribeWhenReady()
        {
            int tries = 0;
            while (!LcIntegration.Instance.SubscribedForEvents && tries++ < 300)
                yield return null;
            LcIntegration.Instance.SubscribeEvents();

            // 5. Engine + HUD + settings-panel singletons, persistent across scene loads.
            var engineGo = new GameObject("TwilightTimer.Core");
            Object.DontDestroyOnLoad(engineGo);
            engineGo.AddComponent<TimerCore>();

            var hudGo = new GameObject("TwilightTimer.Hud");
            Object.DontDestroyOnLoad(hudGo);
            hudGo.AddComponent<TimerHud>();
            hudGo.AddComponent<ProgressIndicatorMover>();

            var lbGo = new GameObject("TwilightTimer.LeaderboardHud");
            Object.DontDestroyOnLoad(lbGo);
            lbGo.AddComponent<LeaderboardHud>();

            var panelGo = new GameObject("TwilightTimer.Panel");
            Object.DontDestroyOnLoad(panelGo);
            panelGo.AddComponent<SettingsPanel>();
        }

        /// <summary>
        /// Copy the shipped default language files (en.txt, zh-Hans.txt) into the
        /// runtime lang dir on first run so the plugin is usable out of the box
        /// and English is always present as the fallback base.
        /// </summary>
        private void EnsureDefaultLangFiles()
        {
            string dir = PersistenceService.LangDir;
            try
            {
                System.IO.Directory.CreateDirectory(dir);
                CopyIfMissing("en.txt");
                CopyIfMissing("zh-Hans.txt");
            }
            catch (System.Exception ex)
            {
                Logger.LogWarning($"TwilightTimer: failed to seed default lang files: {ex.Message}");
            }
        }

        private void CopyIfMissing(string name)
        {
            string dst = System.IO.Path.Combine(PersistenceService.LangDir, name);
            if (System.IO.File.Exists(dst)) return;
            // Defaults live next to the built DLL (csproj CopyToOutputDirectory).
            string src = System.IO.Path.Combine(BepInEx.Paths.PluginPath, PluginInfo.PLUGIN_GUID, "lang", name);
            if (!System.IO.File.Exists(src))
                src = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Info.Location) ?? "", "lang", name);
            if (!System.IO.File.Exists(src)) return;
            try { System.IO.File.Copy(src, dst, overwrite: false); }
            catch (System.Exception ex) { Logger.LogWarning($"TwilightTimer: copy {name} failed: {ex.Message}"); }
        }
    }
}
