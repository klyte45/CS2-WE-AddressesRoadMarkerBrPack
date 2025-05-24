using BridgeAdr;
using BridgeWE;
using Game.Buildings;
using Game.SceneFlow;
using Game.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using Unity.Mathematics;
using Color = UnityEngine.Color;

namespace WE_ARMBRP
{
    public static class ARMBPLayoutsFn
    {
        private enum SignClass
        {
            A,
            R,
            Horizontal,
            Vertical,
        }

        public static string GetImageName(Entity e)
        {
            ExtractParams(e, out var parameters, out var clazz, out var id);
            char charParam2 = (char)('a' + parameters.NumericCustomParam2);
            return clazz switch
            {
                SignClass.R => "R" + id switch
                {
                    4 or
                    5 or
                    6 or
                    8 or
                    24 or
                    25 or
                    35 or
                    36 => id.ToString("00") + charParam2,
                    _ => id.ToString("00")
                },
                SignClass.A => "A" + id switch
                {
                    1 or
                    2 or
                    3 or
                    4 or
                    5 or
                    7 or
                    10 or
                    11 or
                    13 or
                    20 or
                    21 or
                    30 or
                    32 or
                    33 or
                    42 => id.ToString("00") + charParam2,
                    _ => id.ToString("00")
                },
                SignClass.Vertical => "V" + id.ToString("00"),
                SignClass.Horizontal => "H" + id.ToString("00"),
                _ => "X" + id.ToString("00"),
            };
        }

        private static void ExtractParams(Entity e, out ADRData parameters, out SignClass clazz, out int id)
        {
            parameters = ADRRoadMarkerInfoBridge.DesconstructRoadMarkerData(e);
            clazz = (SignClass)((parameters.NumericCustomParam1 & 0xC0) >> 6);
            id = parameters.NumericCustomParam1 & 0x3F;
        }
        public static float3 GetOverallScale(Entity e)
        {
            ExtractParams(e, out var parameters, out var clazz, out var id);
            var scaleIdx = ((parameters.NumericCustomParam1 & 0x300) >> 8);
            return clazz switch
            {
                SignClass.R => scaleIdx switch
                {
                    1 => new float3(.75f, .75f, 1),
                    2 => new float3(.92f, .92f, 1),
                    3 => new float3(1.1f, 1.1f, 1),
                    _ => new float3(.55f, .55f, 1),
                },
                SignClass.A => scaleIdx switch
                {
                    1 => new float3(1f, 1f, 1),
                    2 => new float3(1.25f, 1.25f, 1),
                    3 => new float3(1.5f, 1.5f, 1),
                    _ => new float3(.75f, .75f, 1),
                },
                SignClass.Horizontal => scaleIdx switch
                {
                    1 => new float3(1f, 1f, 1),
                    2 => new float3(1.25f, 1.25f, 1),
                    3 => new float3(1.5f, 1.5f, 1),
                    _ => new float3(.75f, .75f, 1),
                },
                SignClass.Vertical => scaleIdx switch
                {
                    1 => new float3(1.5f, 1.5f, 1),
                    2 => new float3(1.75f, 1.75f, 1),
                    3 => new float3(2f, 2f, 1),
                    _ => new float3(1.25f, 1.25f, 1),
                },
                _ => 0,
            };
        }

        public static SignSettings GetSignSettings(Entity e)
        {
            ExtractParams(e, out var parameters, out var clazz, out var id);
            var classDict = ImagesSettings[clazz];
            var data = classDict.TryGetValue(id, out var signSettings) ? signSettings : classDict[0];
            data.data = parameters;
            data.entity = e;
            return data;
        }

        private static string LengthFormat(ADRData data, Entity e)
        {
            var isImperialSystem = GameManager.instance.settings.userInterface.unitSystem == Game.Settings.InterfaceSettings.UnitSystem.Freedom;
            return $"{(data.NumericCustomParam2 * (isImperialSystem ? 1 : .1f)).ToString(isImperialSystem ? "#,##0" : "#,##0.#", WELocalizationBridge.GetWeCultureInfo())}{(isImperialSystem ? "ft" : "m")}";
        }

        private static string WeightFormat(ADRData data, Entity e)
        {
            var isImperialSystem = GameManager.instance.settings.userInterface.unitSystem == Game.Settings.InterfaceSettings.UnitSystem.Freedom;
            return $"{(data.NumericCustomParam2 * (isImperialSystem ? 1 : .1f)).ToString(isImperialSystem ? "#,##0" : "#,##0.#", WELocalizationBridge.GetWeCultureInfo())}{(isImperialSystem ? "lb" : "t")}";
        }
        private static string MetricSystemDependantFormat(ADRData data, Entity e)
        {
            var isImperialSystem = GameManager.instance.settings.userInterface.unitSystem == Game.Settings.InterfaceSettings.UnitSystem.Freedom;
            return $"{(data.NumericCustomParam2 * (isImperialSystem ? 1 : .1f)).ToString(isImperialSystem ? "#,##0" : "#,##0.#", WELocalizationBridge.GetWeCultureInfo())}";
        }
        private static string IntegerFormat(ADRData data, Entity e)
        {
            return data.NumericCustomParam2.ToString("#,##0", WELocalizationBridge.GetWeCultureInfo());
        }
        private static string GetLengthUnit(ADRData data, Entity e)
        {
            var isImperialSystem = GameManager.instance.settings.userInterface.unitSystem == Game.Settings.InterfaceSettings.UnitSystem.Freedom;
            return isImperialSystem ? "ft" : "m";
        }

        private static string GetVelocityUnit(ADRData data, Entity e)
        {
            var isImperialSystem = GameManager.instance.settings.userInterface.unitSystem == Game.Settings.InterfaceSettings.UnitSystem.Freedom;
            return isImperialSystem ? "mph" : "km/h";
        }
        private static string GetLengthGreaterUnit(ADRData data, Entity e)
        {
            var isImperialSystem = GameManager.instance.settings.userInterface.unitSystem == Game.Settings.InterfaceSettings.UnitSystem.Freedom;
            return isImperialSystem ? "mi" : "km";
        }

        private static ADRHighwayNamingData GetHighwayNamingData(ADRData data, Entity e) => ADRRoadMarkerInfoBridge.GetHighwayRouteNamings(data.RouteDataIndex);

        private static readonly Dictionary<SignClass, Dictionary<int, SignSettings>> ImagesSettings = new()
        {
            [SignClass.A] = new()
            {
                [0] = new SignSettings(),
                [37] = new SignSettings(new TextParams(new(0, 0, 0), 0.138f, 5.072f, LengthFormat)),
                [38] = new SignSettings(new TextParams(new(0, 0.024f, 0), 0.138f, 2.536f, MetricSystemDependantFormat), new(new(0, -.159f, 0), 0.138f, 6.783f, GetLengthUnit)),
                [46] = new SignSettings(new TextParams(new(0, 0.064f, 0), 0.138f, 4.348f, WeightFormat)),
                [47] = new SignSettings(new TextParams(new(0, 0.160f, 0), 0.138f, 3.261f, WeightFormat)),
                [48] = new SignSettings(new TextParams(new(0, -0.153f, 0), 0.138f, 2.174f, LengthFormat)),
            },
            [SignClass.R] = new()
            {
                [0] = new SignSettings(),
                [14] = new SignSettings(new TextParams(new(0, .1f, 0), 0.25f, 2f, WeightFormat)),
                [15] = new SignSettings(new TextParams(default, 0.3f, 2f, LengthFormat)),
                [16] = new SignSettings(new TextParams(new(0, .12f, 0), 0.2f, 2.5f, MetricSystemDependantFormat), new(new(0, -.12f, 0), 0.2f, 2.5f, GetLengthUnit)),
                [17] = new SignSettings(new TextParams(new(0, .175f, 0), 0.18f, 3, WeightFormat)),
                [18] = new SignSettings(new TextParams(new(0, -.175f, 0), 0.125f, 3, LengthFormat)),
                [19] = new SignSettings(new TextParams(new(0, .075f, 0), 0.40f, 1.3f, IntegerFormat), new(new(0, -.25f, 0), 0.1f, 3, GetVelocityUnit)),

            },
            [SignClass.Horizontal] = new()
            {
                [0] = new SignSettings(),
                [1] = new SignSettings(
                    new TextParams(new(0, .25f, 0), .1f, 1.6f, (x, e) => GetHighwayNamingData(x, e).Prefix),
                    new(new(0, .04f, 0), .25f, 2.1f, (x, e) => GetHighwayNamingData(x, e).Suffix),
                      new(new(0, -.25f, 0), .1f, 16, (x, e) => GetHighwayNamingData(x, e).FullName, Color.white)
                    ),
                [2] = new SignSettings(
                      new TextParams(new(0.49f, .2f, 0), .1f, 4, (x, e) => GetHighwayNamingData(x, e).Prefix),
                      new(new(0.49f, -.04f, 0), .2f, 2.1f, (x, e) => GetHighwayNamingData(x, e).Suffix),
                      new(new(-.28f, .16f, 0), .15f, 6.5f, (x, e) =>
                      {
                          var split = GetHighwayNamingData(x, e).FullName.Split(" ");
                          return string.Join(" ", split.Take(split.Length / 2));
                      }, Color.white),
                      new(new(-.28f, -.08f, 0), .15f, 6.5f, (x, e) =>
                      {
                          var split = GetHighwayNamingData(x, e).FullName.Split(" ");
                          return string.Join(" ", split.Skip(split.Length / 2));
                      }, Color.white)
                      )
            },
            [SignClass.Vertical] = new()
            {
                [0] = new SignSettings(),
                [4] = new SignSettings(
                    new TextParams(new(0, .27f, 0), .08f, 4.25f, (x, e) =>
                {
                    if (x.RouteDirection == 0)
                    {
                        var paramsHW = ADRRoadMarkerInfoBridge.GetHighwayRouteNamings(x.RouteDataIndex);
                        return $"{paramsHW.prefix}-{paramsHW.suffix}";
                    }
                    else
                    {
                        return NameSystem.Name.LocalizedName($"K45::ADR.vuio[RouteDirection.{(RouteDirection)x.RouteDirection}]").Translate().ToUpper();
                    }
                }, Color.white),
                    new(new(0, .04f, 0), .1f, 3.75f, GetLengthGreaterUnit, Color.white),
                    new(new(0, -.16f, 0), .1f, 3.75f, GetCurrentMileage, Color.white)),
            }

        };

        private static string GetCurrentMileage(ADRData x, Entity e)
        {
            var isImperialSystem = GameManager.instance.settings.userInterface.unitSystem == Game.Settings.InterfaceSettings.UnitSystem.Freedom;
            return BuildingUtils.GetAddress(World.DefaultGameObjectInjectionWorld.EntityManager, e, out _, out var num)
                        ? Math.Round(num / (isImperialSystem ? 1760f : 1000f)).ToString("#,##0", WELocalizationBridge.GetWeCultureInfo())
                        : "??";
        }

        public struct SignSettings
        {
            public readonly int TextCount => texts?.Length ?? 0;

            public TextParams[] texts = [];
            internal ADRData data = default;
            internal Entity entity = default;

            public SignSettings(params TextParams[] textList) : this()
            {
                texts = textList;
            }

            public readonly string GetTextByIdx(Dictionary<string, string> vars) => GetTextDataByIdx(vars).Content?.Invoke(data, entity) ?? string.Empty;
            public readonly TextParams GetTextDataByIdx(Dictionary<string, string> vars)
            {
                if (TextCount == 0)
                {
                    return default;
                }
                var index = vars.TryGetValue("$idx", out var idx) && int.TryParse(idx, out var intIdx) ? intIdx : -1;
                return index < 0 || index >= TextCount ? default : texts[index];
            }
        }

        public struct TextParams
        {
            internal Func<ADRData, Entity, string> Content;
            public float3 position;
            public float fontScale;
            public Color textColor;
            public float MaxWidthUnscaled;
            public float3 scale => new(fontScale, fontScale, 1);

            internal TextParams(float3 position, float scale, float maxWidth, Func<ADRData, Entity, string> content, Color color = default)
            {
                this.position = position;
                fontScale = scale;
                MaxWidthUnscaled = maxWidth;
                textColor = color == default ? Color.black : color;
                Content = content;
            }
        }

    }
    internal enum RouteDirection
    {
        UNDEFINED,
        NORTH,
        NORTHEAST,
        EAST,
        SOUTHEAST,
        SOUTH,
        SOUTHWEST,
        WEST,
        NORTHWEST,
        INTERNAL,
        EXTERNAL,
    }

    internal record struct ADRData(Colossal.Hash128 RouteDataIndex, int RouteDirection, int NumericCustomParam1, int NumericCustomParam2)
    {
        public static implicit operator (Colossal.Hash128 routeDataIndex, int routeDirection, int numericCustomParam1, int numericCustomParam2)(ADRData value) => (value.RouteDataIndex, value.RouteDirection, value.NumericCustomParam1, value.NumericCustomParam2);

        public static implicit operator ADRData((Colossal.Hash128 routeDataIndex, int routeDirection, int numericCustomParam1, int numericCustomParam2) value) => new(value.routeDataIndex, value.routeDirection, value.numericCustomParam1, value.numericCustomParam2);
    }

    internal record struct ADRHighwayNamingData(string Prefix, string Suffix, string FullName)
    {
        public static implicit operator (string prefix, string suffix, string fullName)(ADRHighwayNamingData value) => (value.Prefix, value.Suffix, value.FullName);

        public static implicit operator ADRHighwayNamingData((string prefix, string suffix, string fullName) value) => new(value.prefix, value.suffix, value.fullName);
    }
}

