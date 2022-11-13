using System.Text.RegularExpressions;
using TacticsogrePspExtracts;

var deserializer = new YamlDotNet.Serialization.Deserializer();
var outputs = deserializer.Deserialize<Dictionary<string, Dictionary<int, List<List<string>>>>>(
    File.ReadAllText(@"..\..\data\texts.yaml"));

var peopleDict = new Dictionary<string, List<string>>();
foreach (var item in outputs["wr_people"].Values)
{
    if (item[0].Count == 4)
    {
        if (item[1].Count == 4)
        {
            var titledName = item[0][2];
            var name2016 = item[1][2].Split('·');
            var name2012 = item[1][3].Split('·');
            foreach (var i in new int[] { 0, name2016.Length - 1 })
            {
                if (titledName.EndsWith(name2016[i]))
                {
                    if (name2016[i] != name2012[i])
                    {
                        item[0][3] = name2012[i];
                    }
                    else
                    {
                        item[0].RemoveAt(3);
                    }
                    break;
                }
            }
        }
        else
        {
            item[0].RemoveAt(3);
        }
    }
    peopleDict.TryAdd(item[0][1], item[0]);
}
peopleDict["天空のギルバルド"][3] = "基尔巴多";
peopleDict.Remove("剣士ハボリム");
var people = peopleDict.Values.ToList();
people[0] = outputs["character"][1][0];

var classDict = new Dictionary<string, List<string>>();
foreach (var item in outputs["class"].Values)
{
    classDict.TryAdd(item[0][1], item[0]);
    classDict.TryAdd(item[1][1], item[1]);
}
var class_ = classDict.Values.TakeWhile(x => x[1] != "デステンプラー").ToList();

var strongpointIds = deserializer.Deserialize<List<int>>(File.ReadAllText(@"..\..\data\strongpoints.yaml"));
var patternHalfwidth = new Regex(@"[0-9]+", RegexOptions.Compiled);
var patternFullwidth = new Regex(@"[０-９]+", RegexOptions.Compiled);
strongpointIds = strongpointIds.Where(x => x < 78).Concat(strongpointIds).ToList();
foreach (var item in outputs["strongpoint"].Values)
{
    item[0] = item[0].Select(text => {
        var asterisked = patternHalfwidth.Replace(patternFullwidth.Replace(text, "＿"), "_");
        return asterisked.FirstOrDefault() != '＿' ? asterisked : text;
    }).ToList();
}
var strongpointDict = new Dictionary<string, List<string>>();
foreach (var id in strongpointIds)
{
    var item = outputs["strongpoint"][id][0];
    strongpointDict.TryAdd(item[1], item);
}
var strongpoint = strongpointDict.Values.ToList();

var clan = outputs["clan"].Values.Select(item => item[0]).ToList();

var translations = new Dictionary<string, List<List<string>>>
{
    { "people", people },
    { "class", class_ },
    { "strongpoint", strongpoint },
    { "clan", clan },
};
foreach (var section in translations.Values)
{
    foreach (var item in section)
    {
        item.Insert(2, "");
    }
}
File.WriteAllText($@"..\..\data\translations.yaml", Util.YamlSerializer.Serialize(translations));
