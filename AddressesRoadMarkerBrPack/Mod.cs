using BridgeAdr;
using BridgeWE;
using Colossal;
using Colossal.IO.AssetDatabase;
using Colossal.Localization;
using Colossal.Logging;
using Game;
using Game.Modding;
using Game.SceneFlow;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace WE_ARMBRP
{
    public class Mod : IMod
    {
        public static ILog log = LogManager.GetLogger($"{typeof(Mod).Assembly.GetName().Name}.{nameof(Mod)}").SetShowsErrorsInUI(false);
        private static readonly BindingFlags allFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly | BindingFlags.GetField | BindingFlags.GetProperty;


        public void OnLoad(UpdateSystem updateSystem)
        {
            log.Info(nameof(OnLoad));

            if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
                log.Info($"Current mod asset at {asset.path}");

            GameManager.instance.RegisterUpdater(DoWhenLoaded);
            GameManager.instance.RegisterUpdater(LoadLocales);
        }

        private void DoWhenLoaded()
        {
            log.Info($"Loading patches");
            DoPatches();
            RegisterModFiles();
        }

        private void RegisterModFiles()
        {
            GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset);
            var modDir = Path.GetDirectoryName(asset.path);

            var imagesDirectory = Path.Combine(modDir, "atlases");
            if (Directory.Exists(imagesDirectory))
            {
                var atlases = Directory.GetDirectories(imagesDirectory, "*", SearchOption.TopDirectoryOnly);
                foreach (var atlasFolder in atlases)
                {
                    WEImageManagementBridge.RegisterImageAtlas(typeof(Mod).Assembly, Path.GetFileName(atlasFolder), Directory.GetFiles(atlasFolder, "*.png"));
                }
            }

            var layoutsDirectory = Path.Combine(modDir, "layouts");
            WETemplatesManagementBridge.RegisterCustomTemplates(typeof(Mod).Assembly, layoutsDirectory);
            WETemplatesManagementBridge.RegisterLoadableTemplatesFolder(typeof(Mod).Assembly, layoutsDirectory);


            var fontsDirectory = Path.Combine(modDir, "fonts");
            WEFontManagementBridge.RegisterModFonts(typeof(Mod).Assembly, fontsDirectory);
        }

        private void DoPatches()
        {
            DoPatch("BelzontWE", new List<(Type, string)>() {
                    (typeof(WEFontManagementBridge), "FontManagementBridge"),
                    (typeof(WEImageManagementBridge), "ImageManagementBridge"),
                    (typeof(WETemplatesManagementBridge), "TemplatesManagementBridge"),
                    (typeof(WELocalizationBridge), "LocalizationBridge")
            });
            DoPatch("AddressesCS2", new List<(Type, string)>() {
                    (typeof(ADRRoadMarkerInfoBridge), "RoadMarkerInfoBridge")
            });
        }

        private void DoPatch(string assemblyName, List<(Type, string)> typesToPatch)
        {
            var weAsset = AssetDatabase.global.GetAsset(SearchFilter<ExecutableAsset>.ByCondition(asset => asset.isEnabled && asset.isLoaded && asset.name.Equals(assemblyName)));
            if (weAsset?.assembly is null)
            {
                throw new Exception($"The module {GetType().Name} requires the mod with dll named '{assemblyName}' to work!");
            }
            var exportedTypes = weAsset.assembly.ExportedTypes;
            foreach (var (type, sourceClassName) in typesToPatch)
            {
                var targetType = exportedTypes.First(x => x.Name == sourceClassName);
                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
                {
                    var srcMethod = targetType.GetMethod(method.Name, allFlags, null, method.GetParameters().Select(x => x.ParameterType).ToArray(), null);
                    if (srcMethod != null) Harmony.ReversePatch(srcMethod, method);
                    else log.Warn($"Method not found while patching WE: {targetType.FullName} {srcMethod.Name}({string.Join(", ", method.GetParameters().Select(x => $"{x.ParameterType}"))})");
                }
            }
        }

        public void OnDispose()
        {
            log.Info(nameof(OnDispose));
        }

        private Queue<(string, IDictionarySource)> previouslyLoadedDictionaries;

        private class ModGenI18n : IDictionarySource
        {
            public ModGenI18n()
            {
            }

            public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
            {
                return new Dictionary<string, string>();
            }

            public void Unload()
            {
            }
        }

        internal void LoadLocales()
        {
            GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset);
            var ModInstallFolder = Path.GetDirectoryName(asset.path);
            var file = Path.Combine(ModInstallFolder, $"i18n/i18n.csv");
            var AdditionalI18nFilesFolder = Path.Combine(ModInstallFolder, $"i18n/");
            previouslyLoadedDictionaries ??= new();
            UnloadLocales();

            var baseModData = new ModGenI18n();
            if (File.Exists(file))
            {
                var fileLines = File.ReadAllLines(file).Select(x => x.Split('\t'));
                var enColumn = Array.IndexOf(fileLines.First(), "en-US");
                var enMemoryFile = new MemorySource(LocaleFileForColumn(fileLines, enColumn));
                foreach (var lang in GameManager.instance.localizationManager.GetSupportedLocales())
                {
                    previouslyLoadedDictionaries.Enqueue((lang, enMemoryFile));
                    GameManager.instance.localizationManager.AddSource(lang, enMemoryFile);
                    if (lang != "en-US")
                    {
                        var valueColumn = Array.IndexOf(fileLines.First(), lang);
                        if (valueColumn > 0)
                        {
                            var i18nFile = new MemorySource(LocaleFileForColumn(fileLines, valueColumn));
                            previouslyLoadedDictionaries.Enqueue((lang, i18nFile));
                            GameManager.instance.localizationManager.AddSource(lang, i18nFile);
                        }
                        else if (File.Exists(Path.Combine(AdditionalI18nFilesFolder, lang + ".csv")))
                        {
                            var csvFileEntries = File.ReadAllLines(Path.Combine(AdditionalI18nFilesFolder, lang + ".csv")).Select(x => x.Split("\t")).ToDictionary(x => x[0], x => x.ElementAtOrDefault(1));
                            var i18nFile = new MemorySource(csvFileEntries);
                            previouslyLoadedDictionaries.Enqueue((lang, i18nFile));
                            GameManager.instance.localizationManager.AddSource(lang, i18nFile);
                        }
                    }
                    previouslyLoadedDictionaries.Enqueue((lang, baseModData));
                    GameManager.instance.localizationManager.AddSource(lang, baseModData);
                }
            }
            else
            {
                foreach (var lang in GameManager.instance.localizationManager.GetSupportedLocales())
                {
                    previouslyLoadedDictionaries.Enqueue((lang, baseModData));
                    GameManager.instance.localizationManager.AddSource(lang, baseModData);
                }
            }
        }
        private static Dictionary<string, string> LocaleFileForColumn(IEnumerable<string[]> fileLines, int valueColumn)
        {
            return fileLines.Skip(1).GroupBy(x => x[0]).Select(x => x.First()).ToDictionary(x => x[0], x => ReplaceSpecialChars(RemoveQuotes(x.ElementAtOrDefault(valueColumn) is string s && !IsNullOrWhitespace(s) ? s : x.ElementAtOrDefault(1))));
        }
        private static string ReplaceSpecialChars(string v)
        {
            return v.Replace("\\n", "\n").Replace("\\t", "\t");
        }
        private static string RemoveQuotes(string v) => v != null && v.StartsWith("\"") && v.EndsWith("\"") ? v[1..^1].Replace("\"\"", "\"") : v;

        private void UnloadLocales()
        {
            while (previouslyLoadedDictionaries.TryDequeue(out var src))
            {
                GameManager.instance.localizationManager.RemoveSource(src.Item1, src.Item2);
            }
        }
        public static bool IsNullOrWhitespace(string str)
        {
            if (!string.IsNullOrEmpty(str))
            {
                for (int i = 0; i < str.Length; i++)
                {
                    if (!char.IsWhiteSpace(str[i]))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
