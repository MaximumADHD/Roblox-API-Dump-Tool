using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Linq;

namespace RobloxApiDumpTool
{
    public class Capabilities
    {
        private List<string> reads = new List<string>();
        private List<string> writes = new List<string>();

        public HashSet<string> Read => reads
            .Except(writes)
            .ToHashSet();

        public HashSet<string> Write => writes
            .Except(reads)
            .ToHashSet();

        public HashSet<string> Unioned => reads
            .Union(writes)
            .ToHashSet();

        public Capabilities(JToken obj = null)
        {
            if (obj == null)
                return;

            if (obj.Type == JTokenType.Array)
            {
                var array = obj.ToObject<JArray>();
                
                foreach (var child in array.Children())
                {
                    var str = child.ToString();
                    writes.Add(str);
                    reads.Add(str);
                }
            }
            else if (obj.Type == JTokenType.Object)
            {
                var read = obj.Value<JArray>("Read");
                var write = obj.Value<JArray>("Write");

                if (read != null)
                {
                    foreach (var child in read.Children())
                    {
                        var str = child.ToString();
                        reads.Add(str);
                    }
                }

                if (write != null)
                {
                    foreach (var child in write.Children())
                    {
                        var str = child.ToString();
                        writes.Add(str);
                    }
                }
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is Capabilities other)
            {
                var readDiff = Read
                    .Except(other.Read)
                    .Any();

                var writeDiff = Write
                    .Except(other.Write)
                    .Any();

                var unionDiff = Unioned
                    .Except(other.Unioned)
                    .Any();

                if (readDiff || writeDiff || unionDiff)
                    return false;

                return true;
            }

            return false;
        }

        public bool IsEmpty()
        {
            return !(Read.Any() || Write.Any() || Unioned.Any());
        }

        public override int GetHashCode()
        {
            return Read.GetHashCode() ^ Write.GetHashCode() ^ Unioned.GetHashCode();
        }

        public string Describe(bool displayUndefined)
        {
            if (Read.Any() || Write.Any())
            {
                var readElements = Read
                    .OrderBy(item => item, StringComparer.Ordinal)
                    .ToArray();

                var writeElements = Write
                    .OrderBy(item => item, StringComparer.Ordinal)
                    .ToArray();

                string read = string.Join(" | ", readElements);
                string write = string.Join(" | ", writeElements);

                if (read != write)
                {
                    var strings = new List<string>();

                    if (read != "" && write != "")
                        strings.Add($"{{🛠️🔎{read}}}");
                    else if (read != "")
                        strings.Add($"{{🛠️{read}}}");


                    if (write != "")
                        strings.Add($"{{🛠️✏️{write}}}");

                    return string.Join(" ", strings);
                }

                if (read != "" && write != "")
                    return $"{{🛠️🔎{read}}}";

                else if (write != "")
                    return $"{{🛠️✏️{write}}}";
                else if (read != "")
                    return $"{{🛠️{read}}}";

                return "";
            }
            else if (Unioned.Any())
            {
                var elements = Unioned
                    .OrderBy(item => item, StringComparer.Ordinal)
                    .ToArray();

                return $"{{🛠️{string.Join(" | ", elements)}}}";
            }
            else
            {
                if (!displayUndefined)
                    return "";

                return "{🛠️Undefined}";
            }
        }

        public override string ToString() => Describe(false);
        public string Value => Describe(true);
    }
}
