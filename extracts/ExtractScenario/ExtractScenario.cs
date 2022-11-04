using System.Text;
using Gibbed.LetUsClingTogether.FileFormats;
using Gibbed.LetUsClingTogether.FileFormats.Text;
using TacticsogrePspExtracts;

var gameData = new GameDataLoader(@"..\..\data\USRDIR-cn\");
var formatter = new Formatter(Util.LoadEncoding(@"..\..\..\imhex-patterns\encodings\tacticsogre-psp-cn2016.tbl"));

var speakerNames = new Dictionary<int, string>();
{
    var data = gameData.Load(900, 20, 1);
    if (data == null) throw new InvalidDataException();
    var stream = new MemoryStream(data);
    var count = BitConverter.ToUInt32(data, 0x04);
    var size = BitConverter.ToInt32(data, 0x0c);
    for (var i = 0; i < count; i++)
    {
        var id = BitConverter.ToInt32(data, 0x10 + i * size);
        var offset = BitConverter.ToInt32(data, 0x10 + i * size + 4);
        stream.Position = offset;
        var s = formatter.Decode(stream, 0);
        speakerNames.Add(id, s);
    }
}

var sceneNames = new Dictionary<uint, string>();
var emeses = new Dictionary<string, List<string>>();
for (var directoryId = 901u; directoryId <= 909u; directoryId++)
{
    gameData.PrepareDirectory(directoryId);
    foreach (var fileEntry in gameData.DirectoryEntry!.Files)
    {
        gameData.PrepareFile(fileEntry);
        gameData.PreparePakd();
        uint[] ids = gameData.ChildIds!;
        var idIndex = new Dictionary<uint, int>();
        for (var i = 0; i < ids.Length; i++) idIndex[ids[i]] = i;
        for (var i = 0; i < ids.Length; i++)
        {
            var id = ids[i];
            if (id >> 15 == 0)
            {
                var script = gameData.ReadChild(i, 0x100);
                var scriptNameOffset = script[0x0c];
                var scriptNameLength = 1;
                while (script[scriptNameOffset + scriptNameLength] != '.') scriptNameLength++;
                var sceneName = Encoding.ASCII.GetString(script.AsSpan(scriptNameOffset, scriptNameLength));
                sceneNames[id] = sceneName;

                if (idIndex.TryGetValue(id | 0x10000, out var emesIndex))
                {
                    var emes = gameData.ReadChild(emesIndex);
                    if (Encoding.ASCII.GetString(emes.AsSpan(0, 4)) != "EMES") throw new InvalidDataException();

                    var messages = new EventMessagesFile();
                    messages.Deserialize(new MemoryStream(emes), formatter);

                    var emesEntry = emeses[sceneName] = new();
                    foreach (var (_, (nameId, text)) in messages.Entries)
                    {
                        emesEntry.Add($"<{speakerNames[nameId]}>\n{text.Replace("{pp}", "").Trim()}".Replace("{hero name}", speakerNames[1]));
                    }
                }
            }
            else break;
        }
    }
}

var chapterNames = new string[] { "1", "2a", "2b", "3a", "3b", "3c", "4", "EP", "DLC" };
for (var chapterIndex = 0u; chapterIndex < chapterNames.Length; chapterIndex++)
{
    var chapterName = chapterNames[chapterIndex];
    foreach (var childId in new uint[] { 4, 5 })
    {
        var emes = gameData.Load(900, 10 + chapterIndex, childId);
        if (emes != null && Encoding.ASCII.GetString(emes.AsSpan(0, 4)) == "EMES")
        {
            var messages = new EventMessagesFile();
            messages.Deserialize(new MemoryStream(emes), formatter);

            var emesEntry = emeses[$"screenplay_{chapterName}_{childId - 3}"] = new();
            foreach (var (id, (_, text)) in messages.Entries)
            {
                emesEntry.Add($"<{id}>\n{text.Replace("{pp}", "").Trim()}");
            }
        }
    }
}

File.WriteAllText(@"..\..\data\sceneNames.yaml", Util.YamlSerializer.Serialize(sceneNames));
File.WriteAllText(@"..\..\data\emeses.yaml", Util.YamlSerializer.Serialize(emeses));
