using System.Reflection;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.EventEmitters;
using Gibbed.LetUsClingTogether.FileFormats.Text;

namespace TacticsogrePspExtracts
{
    public class Util
    {
        public static BaseEncoding LoadEncoding(string tblPath)
        {
            var encoding = new EnglishEncoding();
            var encodingU2C = (Dictionary<char, ushort>)typeof(BaseEncoding)
                .GetField("_UnicodeToEncodedCodepoint", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(encoding)!;
            var encodingC2U = (Dictionary<ushort, char>)typeof(BaseEncoding)
                .GetField("_EncodedCodepointToUnicode", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(encoding)!;
            encodingU2C.Clear();
            encodingC2U.Clear();
            var lines = File.ReadAllLines(tblPath);
            foreach (var line in lines)
            {
                if (line[^2] != '=') throw new InvalidDataException();
                var codepoint = ushort.Parse(line[0..^2], System.Globalization.NumberStyles.HexNumber);
                var character = line[^1];
                encodingU2C.Add(character, codepoint);
                encodingC2U.Add(codepoint, character);
            }
            return encoding;
        }

        public static readonly ISerializer YamlSerializer = new SerializerBuilder()
            .WithEventEmitter(nextEmitter => new MultilineScalarFlowStyleEmitter(nextEmitter))
            .Build();
    }

    class MultilineScalarFlowStyleEmitter : ChainedEventEmitter
    {
        public MultilineScalarFlowStyleEmitter(IEventEmitter nextEmitter)
            : base(nextEmitter) { }

        public override void Emit(ScalarEventInfo eventInfo, IEmitter emitter)
        {
            if (typeof(string).IsAssignableFrom(eventInfo.Source.Type))
            {
                string? value = eventInfo.Source.Value as string;
                if (!string.IsNullOrEmpty(value))
                {
                    bool isMultiLine = value.IndexOfAny(new char[] { '\r', '\n', '\x85', '\x2028', '\x2029' }) >= 0;
                    if (isMultiLine)
                    {
                        eventInfo = new ScalarEventInfo(eventInfo.Source)
                        {
                            Style = ScalarStyle.Literal
                        };
                    }
                }
            }

            nextEmitter.Emit(eventInfo, emitter);
        }
    }
}
