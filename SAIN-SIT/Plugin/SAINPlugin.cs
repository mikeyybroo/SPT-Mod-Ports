using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using DrakiaXYZ.VersionChecker;
using SAIN.Components;
using SAIN.Editor;
using SAIN.Helpers;
using SAIN.Layers;
using SAIN.Plugin;
using SAIN.Preset;
using System;
using UnityEngine;
using static SAIN.AssemblyInfo;
using static SAIN.Editor.SAINLayout;

namespace SAIN
{
    public static class AssemblyInfo
    {
        public const string Title = SAINName;
        public const string Description = "Full Revamp of Escape from Tarkov's AI System.";
        public const string Configuration = SPTVersion;
        public const string Company = "";
        public const string Product = SAINName;
        public const string Copyright = "Copyright � 2023 Solarint";
        public const string Trademark = "";
        public const string Culture = "";

        public const int TarkovVersion = 27050;

        public const string EscapeFromTarkov = "EscapeFromTarkov.exe";

        public const string SAINGUID = "me.sol.sain";
        public const string SAINName = "SAIN";
        public const string SAINVersion = "2.1.6";

        public const string SPTGUID = "com.spt-aki.core";
        public const string SPTVersion = "3.7.1";

        public const string WaypointsGUID = "xyz.drakia.waypoints";
        public const string WaypointsVersion = "1.3.1";

        public const string BigBrainGUID = "xyz.drakia.bigbrain";
        public const string BigBrainVersion = "0.3.1";

        public const string LootingBots = "me.skwizzy.lootingbots";
        public const string Realism = "RealismMod";
    }

    [BepInPlugin(SAINGUID, SAINName, SAINVersion)]
    //[BepInDependency(SPTGUID, SPTVersion)]
    //[BepInDependency(BigBrainGUID, BigBrainVersion)]
    //[BepInDependency(WaypointsGUID, WaypointsVersion)]
    [BepInProcess(EscapeFromTarkov)]
    public class SAINPlugin : BaseUnityPlugin
    {
        public static bool DebugMode => EditorDefaults.GlobalDebugMode;
        public static bool DrawDebugGizmos => EditorDefaults.DrawDebugGizmos;
        public static PresetEditorDefaults EditorDefaults => PresetHandler.EditorDefaults;

        public static SoloDecision ForceSoloDecision = SoloDecision.None;
        public static SquadDecision ForceSquadDecision = SquadDecision.None;
        public static SelfDecision ForceSelfDecision = SelfDecision.None;

        private void Awake()
        {
            //if (!VersionChecker.CheckEftVersion(Logger, Info, Config))
            //{
            //    Sounds.PlaySound(EFT.UI.EUISoundType.ErrorMessage);
            //    throw new Exception("Invalid EFT Version");
            //}

            //new DefaultBrainsClass();

            PresetHandler.Init();
            BindConfigs();
            Patches();
            BigBrainHandler.Init();
            Vector.Init();
        }

        private void BindConfigs()
        {
            string category = "SAIN Editor";

            NextDebugOverlay = Config.Bind(category, "Next Debug Overlay", new KeyboardShortcut(KeyCode.LeftBracket), "Change The Debug Overlay with DrakiaXYZs Debug Overlay");
            PreviousDebugOverlay = Config.Bind(category, "Previous Debug Overlay", new KeyboardShortcut(KeyCode.RightBracket), "Change The Debug Overlay with DrakiaXYZs Debug Overlay");

            OpenEditorButton = Config.Bind(category, "Open Editor", false, "Opens the Editor on press");
            OpenEditorConfigEntry = Config.Bind(category, "Open Editor Shortcut", new KeyboardShortcut(KeyCode.F6), "The keyboard shortcut that toggles editor");

            PauseConfigEntry = Config.Bind(category, "Pause Button", new KeyboardShortcut(KeyCode.Pause), "Pause The Game");
        }

        public static ConfigEntry<KeyboardShortcut> NextDebugOverlay { get; private set; }
        public static ConfigEntry<KeyboardShortcut> PreviousDebugOverlay { get; private set; }
        public static ConfigEntry<bool> OpenEditorButton { get; private set; }
        public static ConfigEntry<KeyboardShortcut> OpenEditorConfigEntry { get; private set; }
        public static ConfigEntry<KeyboardShortcut> PauseConfigEntry { get; private set; }

        private void Patches()
        {
            new UpdateEFTSettingsPatch().Enable();

            try
            {
                new Patches.Generic.KickPatch().Enable();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
            try
            {
                new Patches.Generic.KickPatch().Enable();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
            try
            {
                new Patches.Generic.GetBotController().Enable();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
            try
            {
                new Patches.Generic.GetBotSpawner().Enable();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
            try
            {
                new Patches.Generic.GrenadeThrownActionPatch().Enable();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
            try
            {
                new Patches.Generic.GrenadeExplosionActionPatch().Enable();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
            try
            {
                new Patches.Generic.BotGroupAddEnemyPatch().Enable();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
            try
            {
                new Patches.Generic.BotMemoryAddEnemyPatch().Enable();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
            try
            {
                new Patches.Hearing.TryPlayShootSoundPatch().Enable();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
            try
            {
                new Patches.Hearing.HearingSensorPatch().Enable();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
            try
            {
                new Patches.Hearing.BetterAudioPatch().Enable();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
            try
            {
                new Patches.Talk.PlayerTalkPatch().Enable();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
            try
            {
                new Patches.Talk.TalkDisablePatch1().Enable();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
            try
            {
                new Patches.Talk.TalkDisablePatch2().Enable();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
            try
            {
                new Patches.Talk.TalkDisablePatch3().Enable();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
            try
            {
                new Patches.Talk.TalkDisablePatch4().Enable();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
            try
            {
                new Patches.Vision.NoAIESPPatch().Enable();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
            try
            {
                new Patches.Vision.VisionSpeedPatch().Enable();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
            try
            {
                new Patches.Vision.VisibleDistancePatch().Enable();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
            try
            {
                new Patches.Vision.CheckFlashlightPatch().Enable();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
            try
            {
                new Patches.Shoot.AimTimePatch().Enable();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
            try
            {
                new Patches.Shoot.AimOffsetPatch().Enable();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
            try
            {
                new Patches.Shoot.RecoilPatch().Enable();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
            try
            {
                new Patches.Shoot.LoseRecoilPatch().Enable();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
            try
            {
                new Patches.Shoot.EndRecoilPatch().Enable();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
            try
            {
                new Patches.Shoot.FullAutoPatch().Enable();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
            try
            {
                new Patches.Shoot.SemiAutoPatch().Enable();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }

            try
            {
                new Patches.Components.AddComponentPatch().Enable();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
}

        public static SAINPresetClass LoadedPreset => PresetHandler.LoadedPreset;

        public static SAINBotControllerComponent BotController => GameWorldHandler.SAINBotController;

        private void Update()
        {
            DebugGizmos.Update();
            DebugOverlay.Update();
            ModDetection.Update();
            SAINEditor.Update();
            GameWorldHandler.Update();

            LoadedPreset.GlobalSettings.Personality.Update();
        }

        private void Start() => SAINEditor.Init();

        private void LateUpdate() => SAINEditor.LateUpdate();

        private void OnGUI() => SAINEditor.OnGUI();
    }

    public static class ModDetection
    {
        static ModDetection()
        {
            ModsCheckTimer = Time.time + 5f;
        }

        public static void Update()
        {
            if (!ModsChecked && ModsCheckTimer < Time.time && ModsCheckTimer > 0)
            {
                ModsChecked = true;
                CheckPlugins();
            }
        }

        public static bool LootingBotsLoaded { get; private set; }
        public static bool RealismLoaded { get; private set; }

        public static void CheckPlugins()
        {
            if (Chainloader.PluginInfos.ContainsKey(LootingBots))
            {
                LootingBotsLoaded = true;
                Logger.LogInfo($"SAIN: Looting Bots Detected.");
            }
            if (Chainloader.PluginInfos.ContainsKey(Realism))
            {
                RealismLoaded = true;
                Logger.LogInfo($"SAIN: Realism Detected.");

                // If Realism mod is loaded, we need to adjust how powerlevel is calculated to take into account armor class going up to 10 instead of 6
                // 7 is the default
                EFTCoreSettings.UpdateArmorClassCoef(4f);
            }
            else
            {
                EFTCoreSettings.UpdateArmorClassCoef(7f);
            }
        }

        public static void ModDetectionGUI()
        {
            BeginVertical();

            BeginHorizontal();
            IsDetected(LootingBotsLoaded, "Looting Bots");
            IsDetected(RealismLoaded, "Realism Mod");
            EndHorizontal();

            EndVertical();
        }

        private static void IsDetected(bool value, string name)
        {
            Label(name);
            Box(value ? "Detected" : "Not Detected");
        }

        private static readonly float ModsCheckTimer = -1f;
        private static bool ModsChecked = false;
    }
}
