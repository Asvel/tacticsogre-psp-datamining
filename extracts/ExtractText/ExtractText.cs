using System.Text;
using Gibbed.LetUsClingTogether.FileFormats.Text;
using TacticsogrePspExtracts;

var langs = new string[] { "en", "jp", "cn", "cx" };
var gameDatas = new Dictionary<string, GameDataLoader>
{
    { "en", new GameDataLoader(@"..\..\data\USRDIR-en\") },
    { "jp", new GameDataLoader(@"..\..\data\USRDIR-jp\") },
    { "cn", new GameDataLoader(@"..\..\data\USRDIR-cn\") },
    { "cx", new GameDataLoader(@"..\..\data\USRDIR-cx\") },
};
var formaters = new Dictionary<string, Formatter>
{
    { "en", Formatter.ForEN() },
    { "jp", Formatter.ForJP() },
    { "cn", new Formatter(Util.LoadEncoding(@"..\..\..\imhex-patterns\encodings\tacticsogre-psp-cn2016.tbl")) },
    { "cx", new Formatter(Util.LoadEncoding(@"..\..\..\imhex-patterns\encodings\tacticsogre-psp-cn2012.tbl")) },
};

var inputs = new Input[] {
    new(1000, 1, 20, new[]{ 0 }, "character"),
    new(500, 10241, 0, new[]{ 0, 1 }, "strongpoint"),
    new(1000, 1, 7, new[]{ 1, 2, 3 }, "class"),
    new(900, 20, 1, new[]{ 1 }, "screenplay_character"),
    new(1000, 1, 27, new[]{ 1, 2 }, "battle"),
    new(500, 10241, 12, new[]{ 0, 1 }, "appellation"),
    new(500, 10241, 9, new[]{ 1 }, "unit_condition_help"),
    new(700, 0, 0, new[]{ 0 }, "wr_menu"),
    new(700, 1, 1, new[]{ 3 }, "wr_battlestage"),
    new(700, 1, 2, new[]{ 1, 3 }, "wr_ca"),
    new(700, 1, 3, new[]{ 1, 3 }, "wr_rumor"),
    new(700, 0, 3, new[]{ 3, 4, 5 }, "wr_people"),
    new(700, 0, 5, new[]{ 1, 3 }, "wr_guide_basic"),
    new(700, 0, 6, new[]{ 1, 3 }, "wr_guide_tactic"),
};
var outputs = new Dictionary<string, Dictionary<int, List<List<string>>>>();

foreach (var input in inputs)
{
    var output = outputs[input.Name] = new();
    foreach (var lang in langs)
    {
        var data = gameDatas[lang].Load(input.DirectoryId, input.FileId, input.ChildId);
        if (data == null || Encoding.ASCII.GetString(data.AsSpan(0, 4)) != "xlce") throw new InvalidDataException();
        var stream = new MemoryStream(data);
        var count = BitConverter.ToUInt32(data, 4);
        var size = (int)BitConverter.ToUInt32(data, 12);
        var formatter = formaters[lang];
        for (var i = 0; i < count; i++)
        {
            if (lang == langs[0])
            {
                output.Add(i, new());
            }
            if (i >= output.Count) break;
            var item = output[i];

            for (var j = 0; j < input.Fields.Length; j++)
            {
                if (lang == langs[0])
                {
                    item.Add(new());
                }
                var start = (int)BitConverter.ToUInt32(data, 0x10 + size * i + 0x4 * input.Fields[j]);
                string s;
                stream.Position = start;
                try
                {
                    s = formatter.Decode(stream, 0);
                }
                catch
                {
                    var end = start;
                    while (data[end] != 0) end++;
                    s = BitConverter.ToString(data, start, end - start + 1);
                }
                item[j].Add(s);
            }
        }
    }
    foreach (var kvp in output)
    {
        if (kvp.Value[0].Count != langs.Length)
        {
            output.Remove(kvp.Key);
            continue;
        }
        if (kvp.Value.All(v => v[1] == "" || v[1] == "Not Use" || v[1] == "NOT USED" || v[1] == "nothing" || v[1] == "extra row"))
        {
            output.Remove(kvp.Key);
        }
        else if (input.Name == "battle" && kvp.Value[1][0] == "")
        {
            var s = kvp.Value[0][1];
            if (s[^2] == '+' || s[^1] == 'Ⅱ' || s[^1] == 'Ⅲ' || s[^1] == 'Ⅳ' || s[^1] == '改')
            {
                output.Remove(kvp.Key);
            }
        }
        foreach (var strings in kvp.Value)
        {
            if (strings[2] == strings[3].Replace('「', '“').Replace('」', '”').Replace('『', '“').Replace('』', '”'))
            {
                strings.RemoveAt(3);
            }
        }
    }
}

outputs["clan"] = new()
{
    { 1, new() { new() { "Walister", "ウォルスタ", "瓦尔斯塔" } } },
    { 2, new() { new() { "Galgastan", "ガルガスタン", "加尔加斯坦" } } },
    { 3, new() { new() { "Bakram", "バクラム", "巴库拉姆" } } },
    { 4, new() { new() { "Xenobia", "ゼノビア", "泽诺比亚" } } },
    { 5, new() { new() { "Lodis", "ローディス", "罗迪斯" } } },
    { 6, new() { new() { "Bolmocca", "ボルマウカ", "波尔毛卡" } } },
    { 7, new() { new() { "Balboede", "バルバウダ", "巴尔巴乌达" } } },
};

var yaml = Util.YamlSerializer.Serialize(outputs).Replace("ξοπλρκσ", "C.H.A.R.I.O.T.");
File.WriteAllText($@"..\..\data\texts.yaml", yaml);


record struct Input(uint DirectoryId, uint FileId, uint ChildId, int[] Fields, string Name);
