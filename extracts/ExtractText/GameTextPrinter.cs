namespace TacticsogrePspExtracts
{
    public class GameTextPrinter : IFormatProvider, ICustomFormatter
    {
        public object? GetFormat(Type? type) => type == typeof(ICustomFormatter) ? this : null;
        public string Format(string? fmt, object? arg, IFormatProvider? formatProvider) => fmt switch
        {
            "+-" => FormatPlusMinus(arg),
            "ap" => Convert.ToUInt16(arg) > 100 ? arg!.ToString()! : GetText("appellation", arg),
            "bo" => arg?.Equals(true) == true ? "true" : "false",
            "ch" => GetText("character", arg, unitNameMap),
            "cl" => GetText("clan", arg),
            "op" => compareOperators[Convert.ToByte(arg)],
            "pe" => $"{"LNC"[Convert.ToUInt16(arg)]}",
            "sp" => GetText("strongpoint", arg),
            "sm" => GetText("strongpoint", arg, strongpointMiniNameMap),
            "wr" => GetText("wr_rumor", arg),
            _ when fmt?[0] == '!' => arg?.ToString()?.Equals(fmt?[1..]) == true ? "" : "<error>",
            _ => arg is IFormattable arg_
                ? arg_.ToString(fmt, formatProvider)
                : arg?.ToString() ?? "",
        };

        readonly Dictionary<string, Dictionary<ushort, List<List<string>>>> texts;
        readonly Dictionary<ushort, ushort> unitNameMap = new();
        readonly Dictionary<ushort, ushort> strongpointMiniNameMap = new();
        readonly int langIndex;
        public GameTextPrinter(GameDataLoader gameData, int langIndex)
        {
            this.langIndex = langIndex;

            var deserializer = new YamlDotNet.Serialization.Deserializer();
            void loadYaml<T>(string path, out T v) => v = deserializer.Deserialize<T>(File.ReadAllText(path));
            loadYaml(@"..\..\data\texts.yaml", out texts);

            // unit -> character name
            gameData.PrepareDirectory(1001);
            foreach (var fileEntry in gameData.DirectoryEntry!.Files)
            {
                gameData.PrepareFile(fileEntry);
                gameData.PreparePakd();
                var data = gameData.ReadChild(0);
                var count = BitConverter.ToInt32(data, 4);
                var entrySize = BitConverter.ToInt32(data, 12);
                if (entrySize != 0xa4) throw new InvalidDataException();
                for (var i = 0; i < count; i++)
                {
                    var entryStart = 0x10 + i * entrySize;
                    var nameId = BitConverter.ToUInt16(data, entryStart + 0);
                    var unitId = BitConverter.ToUInt16(data, entryStart + 2);
                    if (nameId == 0 || nameId == unitId) continue;
                    if (unitId == 1) continue;
                    if (!unitNameMap.TryGetValue(unitId, out var prev))
                    {
                        unitNameMap.Add(unitId, nameId);
                    }
                    else
                    {
                        // only Punkin conflicts
                        // if (nameId != prev) throw new InvalidDataException();
                    }
                }
            }

            // strongpoint mini -> strongpoint name
            gameData.PrepareDirectory(800);
            gameData.PrepareFile(256);
            gameData.PreparePakd();
            for (var childIndex = 0; childIndex < gameData.ChildIds!.Length; childIndex++)
            {
                if (gameData.ChildIds[childIndex] % 2 != 1) continue;
                var data = gameData.ReadChild(childIndex);
                var count = BitConverter.ToInt32(data, 4);
                for (int i = 0; i < count; i++)
                {
                    var entryStart = 0x10 + i * 0x10;
                    var minimapId = BitConverter.ToUInt16(data, entryStart + 2);
                    var nameId = BitConverter.ToUInt16(data, entryStart + 4);
                    if (nameId == 0) continue;
                    if (!strongpointMiniNameMap.TryGetValue(minimapId, out var prev))
                    {
                        strongpointMiniNameMap.Add(minimapId, nameId);
                    }
                    else
                    {
                        if (nameId != prev) throw new InvalidDataException();
                    }
                }
            }
        }

        string GetText(string type, object? arg, Dictionary<ushort, ushort>? idMap = null)
        {

            var id = Convert.ToUInt16(arg);
            return idMap?.TryGetValue(id, out var nameId) == true
                ? $"{id}->{nameId}:{texts[type][nameId][0][langIndex]}"
                : $"{id}:{(texts[type].TryGetValue(id, out var text) ? text[0][langIndex] : "Unknown")}";
        }

        static readonly string[] compareOperators = { "==", "!=", "<", "<=", ">", ">=" };

        static string FormatPlusMinus(object? arg)
        {
            var n = Convert.ToInt32(arg);
            return n >= 0 ? $"+= {n}" : $"-= {-n}";
        }

    }
}
