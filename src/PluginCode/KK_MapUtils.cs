using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine.UI;
using UnityEngine.Audio;
//plugin made with the help of IllusionMods/PluginTemplate https://github.com/IllusionMods/PluginTemplate and helpful people in the Koikatsu fan discord https://universalhentai.com/
namespace KK_MapUtils
{
    [BepInPlugin(GUID, PluginName, Version)]
    [BepInProcess("KoikatuVR")]
    [BepInProcess("Koikatu")]
    [BepInProcess("CharaStudio")]
    [BepInProcess("Koikatsu Party")]
    [BepInProcess("Koikatsu Party VR")]
    public class KK_MapUtils : BaseUnityPlugin
    {
        public const string PluginName = "KK_MapUtils";

        public const string GUID = "koikdaisy.kkmaputils";

        public const string Version = "1.0.1";

        internal static new ManualLogSource Logger;

        private static ConfigEntry<bool> _KK_MapUtils_Enabled;

        private void Awake()
        {
            Logger = base.Logger;

            _KK_MapUtils_Enabled = Config.Bind("Enable Plugin", "Enabled", true, $"Plugin for adjusting various map options set by map makers. Looks for gameobjects in map scenes with specific names. Any object named 'KK_MapUtils_DisableCharaLight' disables the character light when a scene is loaded. Point lights named 'KK_MapUtils_Day', 'KK_MapUtils_Evening', and 'KK_MapUtils_Night' will determine the ambient light color of the scene.");

            if (_KK_MapUtils_Enabled.Value)
            {
                Harmony.CreateAndPatchAll(typeof(Hooks), GUID);
            }
        }

        private static class Hooks
        {
            //STUDIO MAP SWITCH HOOK
            [HarmonyPostfix]
            [HarmonyPatch(typeof(Studio.Map), "LoadMapCoroutine")]
            private static void StudioDisableCharaLight(Studio.Map __instance)
            {
                int mapID = __instance.no;
                __instance.StartCoroutine(Controller.WaitForNull(Controller.WaitForStudioLoaded(__instance, mapID), __instance));
            }

            //STUDIO MAP TIME CHANGE HOOK
            [HarmonyPostfix]
            [HarmonyPatch(typeof(Studio.MapCtrl), "OnClickSunLightType")]
            private static void StudioMapLightChange(Studio.MapCtrl __instance)
            {
                Controller.SetAmbientLight();
            }

            //HSCENE MAIN GAME HOOK
            [HarmonyPostfix]
            [HarmonyPatch(typeof(HScene), "Awake")]
            private static void MainGameDisableCharaLight(HScene __instance)
            {
                __instance.StartCoroutine(Controller.WaitForNull(Controller.WaitForHSceneLoaded(), __instance));
            }

            //HSCENE VR GAME HOOK
            [HarmonyPostfix]
            [HarmonyPatch(typeof(BaseLoader), "Awake")]
            private static void VRDisableCharaLight(BaseLoader __instance)
            {
                if (Process.GetCurrentProcess().ProcessName.Contains("VR") &&
                    GameObject.Find("AssetBundleManager") != null &&
                    GameObject.Find("AssetBundleManager").GetComponent<Manager.Scene>().LoadSceneName == "VRHScene" &&
                    GameObject.Find("AssetBundleManager").GetComponent<Manager.Scene>().PrevLoadSceneName == "VRCharaSelect")
                {
                    __instance.StartCoroutine(Controller.WaitForNull(Controller.WaitForVRLoaded(), __instance));
                }
            }


        }
        private static class Controller
        {
            private static readonly string hSceneLightName = "Directional Light";
            private static readonly string studioLightName = "Directional Chara";
            private static readonly string VRLightName = "Directional light";

            private static readonly string disableCharaLightName = PluginName + "_DisableCharaLight";
            private static readonly string ambientLightName = PluginName + "_Ambient";

            private static readonly string[] VRLightParentNames = new string[] { "koikdaisy.kkcamlocklightvr", "Camera (eye)" };

            //WAITS UNTIL ASSETBUNDLEMANAGER IS OCCUPYING A NULL SCENE, AFTER WHICH IT STARTS ANOTHER SPECIFIED COROUTINE
            public static IEnumerator WaitForNull(IEnumerator iEnumToStart, MonoBehaviour __instance)
            {
                yield return new WaitUntil(() => { return GameObject.Find("AssetBundleManager").GetComponent<Manager.Scene>().baseScene == null; });
                __instance.StartCoroutine(iEnumToStart);

            }

            //STUDIO MAP SWITCHING COROUTINE
            public static IEnumerator WaitForStudioLoaded(Studio.Map __instance, int mapID)
            {
                yield return new WaitUntil(() =>
                {
                    return
                    //execute when loading into new studio map, OR
                    ((GameObject.Find("AssetBundleManager").GetComponent<Manager.Scene>().baseScene != null) ||
                    //execute when unloading to none.
                    (GameObject.Find("AssetBundleManager").GetComponent<Manager.Scene>().baseScene == null && (GameObject.Find("AssetBundleManager").GetComponent<Manager.Scene>().AddSceneName.Length == 0)));
                });
                Logger.Log(LogLevel.Debug, PluginName + ": Loaded studio map, disable chara light: " + (GameObject.Find(disableCharaLightName) != null));
                SetCharaLightEnabled(studioLightName);
                SetAmbientLight();
                RouteMapAudio();

            }

            //HSCENE MAIN GAME COROUTINE
            public static IEnumerator WaitForHSceneLoaded()
            {
                yield return new WaitUntil(() => { return (GameObject.Find("AssetBundleManager").GetComponent<Manager.Scene>().baseScene != null); });
                Logger.Log(LogLevel.Debug, PluginName + ": Loaded HScene, disable chara light: " + (GameObject.Find(disableCharaLightName) != null));
                SetCharaLightEnabled(hSceneLightName);
                SetAmbientLight();
                RouteMapAudio();
            }

            //HSCENE VR GAME COROUTINE
            public static IEnumerator WaitForVRLoaded()
            {
                yield return new WaitUntil(() => { return (GameObject.Find("AssetBundleManager").GetComponent<Manager.Scene>().baseScene != null); });
                Logger.Log(LogLevel.Debug, PluginName + ": Loaded VRHScene, disable chara light: " + (GameObject.Find(disableCharaLightName) != null));
                SetCharaLightEnabledVR(VRLightName, VRLightParentNames);
                SetAmbientLight();
                RouteMapAudio();
            }



            //STUDIO + MAIN GAME EXECUTOR
            private static void SetCharaLightEnabled(string lightName)
            {
                bool enabled = (GameObject.Find(disableCharaLightName) == null);
                if (_KK_MapUtils_Enabled.Value)
                {
                    Light[] lights = FindObjectsOfType<Light>();

                    foreach (Light light in lights)
                    {
                        if (light.gameObject.name == lightName)
                        {
                            light.enabled = enabled;
                            break;
                        }
                    }

                }
            }

            /// <summary>
            /// Checks for the existence of point lights named any of the ambient light keywords and sets the ambient light accordingly.
            /// </summary>
            public static void SetAmbientLight()
            {
                if (!_KK_MapUtils_Enabled.Value) return;
                GameObject setObject = GameObject.Find(ambientLightName);
                Light setLight = null;
                if (setObject != null) { setLight = setObject.GetComponent<Light>(); }
                if (setLight != null)
                {
                    RenderSettings.ambientLight = setLight.color;
                }


            }
            /// <summary>
            /// Finds the ENV audio mixer group and routes all audio sources on the Map layer to it.
            /// </summary>
            public static void RouteMapAudio()
            {
                List<AudioSource> mapSounds = new List<AudioSource>();
                foreach (AudioSource a in Resources.FindObjectsOfTypeAll<AudioSource>())
                {
                    if (a.gameObject.layer == LayerMask.NameToLayer("Map")) mapSounds.Add(a);
                }

                foreach (AudioSource a in Resources.FindObjectsOfTypeAll<AudioSource>())
                {
                    if (a.outputAudioMixerGroup.name == "ENV")
                    {
                        foreach (AudioSource ms in mapSounds)
                        {
                            ms.outputAudioMixerGroup = a.outputAudioMixerGroup;
                        }
                        if (a.outputAudioMixerGroup != null) Logger.Log(LogLevel.Debug, PluginName + $": Routed {mapSounds.Count} Audio Source(s) to 'ENV' AudioMixerGroup.");
                        break;
                    }
                }
            }

            //VR GAME EXECUTOR
            private static void SetCharaLightEnabledVR(string lightName, string[] lightParentNames)
            {
                bool enabled = (GameObject.Find(disableCharaLightName) == null);
                if (_KK_MapUtils_Enabled.Value)
                {
                    Light[] lights = FindObjectsOfType<Light>();

                    foreach (Light light in lights)
                    {
                        foreach (string n in lightParentNames)
                        {
                            if (light.gameObject.name == lightName && light.gameObject.transform.parent.name == n)
                            {
                                light.enabled = enabled;
                                break;
                            }

                        }
                    }
                }
            }
        }
    }
}
