﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Xml;
using HarmonyLib;
using OWML.Common;
using OWML.ModHelper;
using OWML.ModHelper.Events;
using UnityEngine;

namespace GhostTranslations
{
    public class GhostTranslations : ModBehaviour
    {
        /// <summary>The instance of this singleton</summary>
        public static GhostTranslations Instance { get; private set; }

        /// <summary>Awakes the script</summary>
        private void Awake()
        {
            if (Instance != null)
                UnityEngine.Object.Destroy(this);
            Instance = this;
            UnityEngine.Object.DontDestroyOnLoad(this);
        }

        private void Start()
        {
            ModHelper.HarmonyHelper.AddPostfix<GhostWallText>("LateInitialize", typeof(Patches), "GhostWallLateInitialize");
            ModHelper.HarmonyHelper.AddPrefix<GhostWallText>("InitializeXmlAsset", typeof(Patches), "GhostWallInitializeXmlAsset");
            ModHelper.HarmonyHelper.AddPrefix<GhostWallText>("SetNewXmlData", typeof(Patches), "GhostWallSetNewXmlData");
            ModHelper.HarmonyHelper.AddPrefix<GhostWallText>("SetAsTranslated", typeof(Patches), "GhostWallSetAsTranslated");
            ModHelper.HarmonyHelper.AddPrefix<GhostWallText>("GetNumTextBlocksTranslated", typeof(Patches), "GhostWallGetNumTextBlocksTranslated");
            ModHelper.HarmonyHelper.AddPrefix<GhostWallText>("IsTranslated", typeof(Patches), "GhostWallIsTranslated");
            ModHelper.HarmonyHelper.AddPrefix<GhostWallText>("GetNumTextBlocks", typeof(Patches), "GhostWallGetNumTextBlocks");
            ModHelper.HarmonyHelper.AddPrefix(AccessTools.Method(typeof(GhostWallText), "GetTextNode", new Type[1] { typeof(int) }), typeof(Patches), "GhostWallTextNode");
            ModHelper.HarmonyHelper.Transpile<NomaiTranslator>("Update", typeof(Patches), "GhostTranslatorTranspiler");
            ModHelper.HarmonyHelper.Transpile<NomaiTranslatorProp>("DisplayTextNode", typeof(Patches), "GhostTranslatorDisplayTranspiler");
            ModHelper.Console.WriteLine($"{nameof(GhostTranslations)} is ready to go!", MessageType.Success);
            LoadManager.OnCompleteSceneLoad += (scene, loadScene) =>
            {
                if (loadScene != OWScene.SolarSystem) return;
                GameObject Arc1 = InstantiateInactive(GameObject.Find("RingWorld_Body/Sector_RingInterior/Sector_Zone2/Interactables_Zone2/Props_IP_ZoneSign_2/Arc_TestAlienWriting/Arc 1"));
                GameObject DreamWorldDecal = GameObject.Find("DreamWorld_Body/Sector_DreamWorld/Sector_Underground/Interactibles_Underground/SarcophagusController/SarcophagusInterface/Decal_DW_Text");
                if (DreamWorldDecal != null)
                {
                    Arc1.transform.parent = DreamWorldDecal.transform;
                    Arc1.transform.localPosition = Vector3.zero;
                    Arc1.transform.localRotation = Quaternion.identity;
                    Arc1.transform.localScale = Vector3.one;
                    Arc1.SetActive(true);
                    BoxCollider collider = DreamWorldDecal.AddComponent<BoxCollider>();
                    collider.center = Vector3.zero;
                    collider.size = new Vector3(0.75f, 0.5f, 0.25f);
                    collider.isTrigger = true;
                    collider.contactOffset = 0.01f;
                    GhostWallText wallText = DreamWorldDecal.AddComponent<GhostWallText>();
                    wallText._minimumReadableDistance = 1;
                    wallText._interactRange = 5;
                    wallText._turnOffFlashlight = false;
                    wallText._textLine = Arc1.GetAddComponent<NomaiTextLine>();
                }
            };
        }

        public static GameObject InstantiateInactive(GameObject original)
        {
            var state = original.activeSelf;
            original.SetActive(false);
            var newObj = UnityEngine.Object.Instantiate(original);
            newObj.name = original.name;
            original.SetActive(state);
            return newObj;
        }

        internal static T Load<T>(string filename) => Instance.ModHelper.Storage.Load<T>(filename);

        internal static void Save<T>(T obj, string filename) => Instance.ModHelper.Storage.Save<T>(obj, filename);

        internal static void Log(string message) => Instance.ModHelper.Console.WriteLine(message, MessageType.Message);

        internal static void LogInfo(string message) => Instance.ModHelper.Console.WriteLine(message, MessageType.Info);

        internal static void LogSuccess(string message) => Instance.ModHelper.Console.WriteLine(message, MessageType.Success);

        internal static void LogWarning(string message) => Instance.ModHelper.Console.WriteLine(message, MessageType.Warning);

        internal static void LogError(string message) => Instance.ModHelper.Console.WriteLine(message, MessageType.Error);

        internal static void LogFatal(string message) => Instance.ModHelper.Console.WriteLine(message, MessageType.Fatal);

        internal static void LogQuit(string message) => Instance.ModHelper.Console.WriteLine(message, MessageType.Quit);
    }
    public static class Patches
    {
        private static void LoadNewText(this GhostWallText __instance, TextAsset textAsset)
        {
            __instance.SetTextAsset(textAsset);
            __instance.InitializeXmlAsset();
        }

        private static void ReloadText(this GhostWallText __instance)
        {
            string text = TextFromSector(__instance.GetComponentInParent<Sector>());
            if (!string.IsNullOrWhiteSpace(text))
                __instance.LoadNewText(new NomaiXml(text));
            else
            {
                __instance._dictNomaiTextData.Clear();
                __instance._dictNomaiTextData.Add(1, new NomaiText.NomaiTextData(1, -1, new UntranslatableNode(), true));
                __instance._listDBConditions.Clear();
            }
        }

        private static void LoadNewText(this GhostWallText __instance, NomaiXml xml) => LoadNewText(__instance, xml.ToTextAsset());

        private static string TextFromSector(Sector sector)
        {
            if (sector == null)
            {
                GhostTranslations.LogError($"No sector has been given");
                return string.Empty;
            }
            string name = sector.name.Replace("Sector_","");
            switch (name)
            {
                case "Zone1":
                    return "<color=orange>River Lowlands</color>";
                case "Zone2":
                    return "<color=orange>Cinder Isles</color>";
                case "JammingControlRoom_Zone4":
                    switch (PlayerData.GetSavedLanguage())
                    {
                        case TextTranslation.Language.FRENCH:
                            return "<color=orange>Barrage</color>";
                        case TextTranslation.Language.GERMAN:
                            return "<color=orange>Talsperre</color>";
                        case TextTranslation.Language.ITALIAN:
                            return "<color=orange>Diga</color>";
                        case TextTranslation.Language.JAPANESE:
                            return "<color=orange>ダム</color>";
                        case TextTranslation.Language.KOREAN:
                            return "<color=orange>댐</color>";
                        case TextTranslation.Language.POLISH:
                            return "<color=orange>Zapora</color>";
                        case TextTranslation.Language.PORTUGUESE_BR:
                            return "<color=orange>Barragem</color>";
                        case TextTranslation.Language.RUSSIAN:
                            return "<color=orange>Плотина</color>";
                        case TextTranslation.Language.CHINESE_SIMPLE:
                            return "<color=orange>水坝</color>";
                        case TextTranslation.Language.SPANISH_LA:
                            return "<color=orange>Represa</color>";
                        case TextTranslation.Language.TURKISH:
                            return "<color=orange>Baraj</color>";
                        default:
                            switch (PlayerData.GetSavedLanguage().ToString())
                            {
                                case "Czech":
                                    return "<color=orange>Přehrada</color>";
                                case "Íslenska":
                                    return "<color=orange>Stífla</color>";
                                default:
                                    return "<color=orange>Dam</color>";
                            }
                    }
                case "DarkSideArrival":
                    switch (PlayerData.GetSavedLanguage())
                    {
                        case TextTranslation.Language.FRENCH:
                            return "Entrée du face sombre</color>";
                        case TextTranslation.Language.GERMAN:
                            return "Dunkleseite Eingang</color>";
                        case TextTranslation.Language.ITALIAN:
                            return "Ingresso dalla faccia oscuro</color>";
                        case TextTranslation.Language.JAPANESE:
                            return "暗部の玄関</color>";
                        case TextTranslation.Language.KOREAN:
                            return "어두운면 입구</color>";
                        case TextTranslation.Language.POLISH:
                            return "Ciemnej strony wejście</color>";
                        case TextTranslation.Language.PORTUGUESE_BR:
                            return "Entrada do lado escuro</color>";
                        case TextTranslation.Language.RUSSIAN:
                            return "Чёрный боковой вход</color>";
                        case TextTranslation.Language.CHINESE_SIMPLE:
                            return "暗侧入口</color>";
                        case TextTranslation.Language.SPANISH_LA:
                            return "Entrada del lado oscuro</color>";
                        case TextTranslation.Language.TURKISH:
                            return "Karanlık taraf girişi</color>";
                        default:
                            switch (PlayerData.GetSavedLanguage().ToString())
                            {
                                case "Czech":
                                    return "Vchod z tmavé strany</color>";
                                case "Íslenska":
                                    return "Dökkur hlið inngangur</color>";
                                default:
                                    return "Dark Side Entrance</color>";
                            }
                    }
                case "LightSideArrival":
                    switch (PlayerData.GetSavedLanguage())
                    {
                        case TextTranslation.Language.FRENCH:
                            return "Entrée du face lumineuse";
                        case TextTranslation.Language.GERMAN:
                            return "Helleseite Eingang";
                        case TextTranslation.Language.ITALIAN:
                            return "Ingresso dalla faccia chiaro";
                        case TextTranslation.Language.JAPANESE:
                            return "光部の玄関";
                        case TextTranslation.Language.KOREAN:
                            return "가벼운면 입구";
                        case TextTranslation.Language.POLISH:
                            return "Jasnej strony wejście";
                        case TextTranslation.Language.PORTUGUESE_BR:
                            return "Entrada do lado iluminado";
                        case TextTranslation.Language.RUSSIAN:
                            return "Световой боковой вход";
                        case TextTranslation.Language.CHINESE_SIMPLE:
                            return "明亮侧入口";
                        case TextTranslation.Language.SPANISH_LA:
                            return "Entrada del lado luminoso";
                        case TextTranslation.Language.TURKISH:
                            return "Parlak taraf girişi";
                        default:
                            switch (PlayerData.GetSavedLanguage().ToString())
                            {
                                case "Czech":
                                    return "Vchod z světlé strany";
                                case "Íslenska":
                                    return "Bjart hlið inngangur";
                                default:
                                    return "Light Side Entrance";
                            }
                    }
                case "HiddenGorge":
                    return "<color=orange>Hidden Gorge</color>";
                case "PrisonDocks":
                    switch (PlayerData.GetSavedLanguage())
                    {
                        case TextTranslation.Language.FRENCH:
                            return "<color=orange>ENTRÉE INTERDITE</color>";
                        case TextTranslation.Language.GERMAN:
                            return "<color=orange>EINGANG VERBOTEN</color>";
                        case TextTranslation.Language.ITALIAN:
                            return "<color=orange>ENTRATA VIETATA</color>";
                        case TextTranslation.Language.JAPANESE:
                            return "<color=orange>立入禁止</color>";
                        case TextTranslation.Language.KOREAN:
                            return "<color=orange>출입 금지</color>";
                        case TextTranslation.Language.POLISH:
                            return "<color=orange>ZAKAZ WSTĘPU</color>";
                        case TextTranslation.Language.PORTUGUESE_BR:
                            return "<color=orange>ENTRADA PROIBIDA</color>";
                        case TextTranslation.Language.RUSSIAN:
                            return "<color=orange>ВХОД ВОСПРЕЩЁН</color>";
                        case TextTranslation.Language.CHINESE_SIMPLE:
                            return "<color=orange>禁行</color>";
                        case TextTranslation.Language.SPANISH_LA:
                            return "<color=orange>ENTRADA PROHIBIDA</color>";
                        case TextTranslation.Language.TURKISH:
                            return "<color=orange>GIRILMEZ</color>";
                        default:
                            switch (PlayerData.GetSavedLanguage().ToString())
                            {
                                case "Czech":
                                    return "<color=orange>ZÁKAZ VSTUPU</color>";
                                case "Íslenska":
                                    return "<color=orange>BANNAÐUR AÐGANGUR</color>";
                                default:
                                    return "<color=orange>KEEP OUT</color>";
                            }
                    }
                case "Underground":
                    switch (PlayerData.GetSavedLanguage())
                    {
                        case TextTranslation.Language.FRENCH:
                            return "<color=orange>Sarcophage du prisonnier</color>";
                        case TextTranslation.Language.GERMAN:
                            return "<color=orange>Sarkophag des Häftling</color>";
                        case TextTranslation.Language.ITALIAN:
                            return "<color=orange>Sarcofago del Prigioniero</color>";
                        case TextTranslation.Language.JAPANESE:
                            return "<color=orange>囚人の石棺</color>";
                        case TextTranslation.Language.KOREAN:
                            return "<color=orange>죄수의 석관</color>";
                        case TextTranslation.Language.POLISH:
                            return "<color=orange>Sarkofag więzień</color>";
                        case TextTranslation.Language.PORTUGUESE_BR:
                            return "<color=orange>Sarcófago do Prisioneiro</color>";
                        case TextTranslation.Language.RUSSIAN:
                            return "<color=orange>Саркофаг узника</color>";
                        case TextTranslation.Language.CHINESE_SIMPLE:
                            return "<color=orange>幽禁者的石棺</color>";
                        case TextTranslation.Language.SPANISH_LA:
                            return "<color=orange>Sarcófago del prisionero</color>";
                        case TextTranslation.Language.TURKISH:
                            return "<color=orange>Tutsak Lahit</color>";
                        default:
                            switch (PlayerData.GetSavedLanguage().ToString())
                            {
                                case "Czech":
                                    return "<color=orange>Trestancem sarkofág</color>";
                                case "Íslenska":
                                    return "<color=orange>Sarkófags fanginn</color>";
                                default:
                                    return "<color=orange>Prisoner's Sarcophagus</color>";
                            }
                    }
                default:
                    GhostTranslations.LogError($"No text for sector \"{name}\"");
                    return string.Empty;
            }
        }

        public static void GhostWallLateInitialize(GhostWallText __instance)
        {
            __instance._dictNomaiTextData = new Dictionary<int, NomaiText.NomaiTextData>(ComparerLibrary.intEqComparer);
            __instance._listDBConditions = new List<NomaiText.NomaiTextConditionData>();
            __instance.ReloadText();
        }

        public static bool GhostWallInitializeXmlAsset(GhostWallText __instance)
        {
            if (__instance._nomaiTextAsset == null)
                GhostTranslations.LogWarning("GhostWallText does not have a TextAsset!");
            else
                __instance.LoadXml();
            return false;
        }

        public static bool GhostWallSetNewXmlData(GhostWallText __instance, XmlNode rootNode)
        {
            __instance.VerifyInitialized();
            __instance.LoadTextXml(rootNode);
            return false;
        }

        public static bool GhostWallSetAsTranslated(GhostWallText __instance, int id)
        {
            __instance.VerifyInitialized();
            if (__instance._dictNomaiTextData.ContainsKey(id))
            {
                if (!__instance._dictNomaiTextData[id].IsTranslated)
                {
                    NomaiText.NomaiTextData nomaiTextData1 = __instance._dictNomaiTextData[id];
                    nomaiTextData1.IsTranslated = true;
                    NomaiText.NomaiTextData nomaiTextData2 = nomaiTextData1;
                    __instance._dictNomaiTextData[id] = nomaiTextData2;
                    __instance.CheckSetDatabaseCondition();
                }
            }
            else
                GhostTranslations.LogError("ID does not exist in GhostWallText, cannot set as translated");
            return false;
        }

        public static bool GhostWallGetNumTextBlocksTranslated(
          GhostWallText __instance,
          ref int __result)
        {
            __instance.VerifyInitialized();
            int num = 0;
            foreach (KeyValuePair<int, NomaiText.NomaiTextData> keyValuePair in __instance._dictNomaiTextData)
            {
                if (keyValuePair.Value.IsTranslated)
                    ++num;
            }
            __result = num;
            return false;
        }

        public static bool GhostWallIsTranslated(GhostWallText __instance, ref bool __result, int id)
        {
            __instance.VerifyInitialized();
            __result = __instance._dictNomaiTextData.ContainsKey(id) && __instance._dictNomaiTextData[id].IsTranslated;
            return false;
        }

        public static bool GhostWallGetNumTextBlocks(GhostWallText __instance, ref int __result)
        {
            __instance.VerifyInitialized();
            __result = __instance._dictNomaiTextData.Count;
            return false;
        }

        public static bool GhostWallTextNode(GhostWallText __instance, ref string __result, int id)
        {
            __instance.VerifyInitialized();
            if (__instance._dictNomaiTextData.ContainsKey(id))
            {
                XmlNode node = __instance._dictNomaiTextData[id].TextNode;
                if (node is UntranslatableNode)
                    __result = UITextLibrary.GetString(UITextType.TranslatorUntranslatableWarning);
                else
                    __result = TextTranslation.Translate(__instance._dictNomaiTextData[id].TextNode.InnerText);
            }
            else
                __result = UITextLibrary.GetString(UITextType.TranslatorUntranslatableWarning);
            return false;
        }

        private static bool IsUntranslatable(this GhostWallText ghostWallText)
        {
            if (ghostWallText == null) return true;
            if (ghostWallText._dictNomaiTextData.Count == 0) return true;
            if (ghostWallText._dictNomaiTextData[1].TextNode is UntranslatableNode) return true;
            return false;
        }

        public static void GhostTranslator(
          NomaiTranslator translator,
          NomaiTranslatorProp _translatorProp,
          GhostWallText _currentNomaiText)
        {
            if (!_currentNomaiText.IsUntranslatable())
                _translatorProp.SetTargetingGhostText(false);
            _translatorProp.SetNomaiText(_currentNomaiText, _currentNomaiText.GetTextLine().GetEntryID());
        }

        public static IEnumerable<CodeInstruction> GhostTranslatorTranspiler(
          IEnumerable<CodeInstruction> instructions,
          ILGenerator generator,
          MethodBase methodBase)
        {
            using (IEnumerator<CodeInstruction> instructionsEnumerator = instructions.GetEnumerator())
            {
                CodeInstruction previous = null;
                while (instructionsEnumerator.MoveNext())
                {
                    CodeInstruction instruction = instructionsEnumerator.Current;
                    yield return instruction;
                    if (instruction.opcode == OpCodes.Callvirt && instruction.operand is MethodInfo mi && mi.Name == "SetTargetingGhostText")
                    {
                        if (previous != null)
                        {
                            if (previous.opcode == OpCodes.Ldc_I4_1)
                                goto code;
                            else
                            {
                                if (previous.opcode == OpCodes.Ldc_I4_S && previous.operand is int i_s)
                                {
                                    if (i_s == 1)
                                        goto code;
                                }
                                else if (previous.opcode == OpCodes.Ldc_I4 && previous.operand is int i)
                                {
                                    if (i == 1)
                                        goto code;
                                }
                            }
                        }
                    }
                    goto next;
                code:
                    yield return new CodeInstruction(OpCodes.Nop);
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(NomaiTranslator), "_translatorProp"));
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 9);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Patches), "GhostTranslator"));
                    yield return new CodeInstruction(OpCodes.Nop);
                next:
                    previous = instruction;
                }
            }
        }

        public static string ReplaceNomai_IfOwelk(string text, NomaiText component)
        {
            if (component is GhostWallText)
                switch (PlayerData.GetSavedLanguage())
                    {
                        case TextTranslation.Language.FRENCH:
                            return text.Replace("nomaï", "hiboupiti");
                        case TextTranslation.Language.GERMAN:
                            return text.Replace("Nomai", "Euleëlch");
                        case TextTranslation.Language.ITALIAN:
                            return text.Replace("nomai", "gufolce");
                        case TextTranslation.Language.JAPANESE:
                            return text.Replace("Nomai", "フクロジカ");
                        case TextTranslation.Language.KOREAN:
                            return text.Replace("노마이", "올빼미고라니");
                        case TextTranslation.Language.POLISH:
                            return text.Replace("Nomai", "Sowałoś");
                        case TextTranslation.Language.PORTUGUESE_BR:
                            return text.Replace("Nomai", "Corujalce");
                        case TextTranslation.Language.RUSSIAN:
                            return text.Replace("номаи", "совалось");
                        case TextTranslation.Language.CHINESE_SIMPLE:
                            return text.Replace("挪麦", "猫头鹰麋鹿");
                        case TextTranslation.Language.SPANISH_LA:
                            return text.Replace("nomai", "búhoalce");
                        case TextTranslation.Language.TURKISH:
                            return text.Replace("Nomai", "Baykuyik");
                        default:
                            switch (PlayerData.GetSavedLanguage().ToString())
                            {
                                case "Czech":
                                    return text.Replace("Nomai", "Sovalos");
                                case "Íslenska":
                                    return text.Replace("Nómæ", "Elgugla");
                                default:
                                    return text.Replace("Nomai", "Owlk");
                            }
                    }
            return text;
        }

        public static IEnumerable<CodeInstruction> GhostTranslatorDisplayTranspiler(
          IEnumerable<CodeInstruction> instructions,
          ILGenerator generator,
          MethodBase methodBase)
        {
            using (IEnumerator<CodeInstruction> instructionsEnumerator = instructions.GetEnumerator())
            {
                CodeInstruction previous = null;
                while (instructionsEnumerator.MoveNext())
                {
                    CodeInstruction instruction = instructionsEnumerator.Current;
                    if (instruction.opcode == OpCodes.Stloc_0)
                        goto code;
                    else if (instruction.opcode == OpCodes.Stloc_S && instruction.operand is int i_s && i_s == 0)
                        goto code;
                    else if (instruction.opcode == OpCodes.Stloc && instruction.operand is int i && i == 0)
                        goto code;
                    else
                    {
                        yield return instruction;
                        goto next;
                    }
                code:
                    CodeInstruction ldarg = new CodeInstruction(OpCodes.Ldarg_0);
                    ldarg.blocks = instruction.blocks;
                    ldarg.labels = instruction.labels;
                    yield return ldarg;
                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(NomaiTranslatorProp), nameof(NomaiTranslatorProp._nomaiTextComponent)));
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Patches), nameof(Patches.ReplaceNomai_IfOwelk)));
                    yield return new CodeInstruction(OpCodes.Stloc_0);
                next:
                    previous = instruction;
                }
            }
        }
    }
}
