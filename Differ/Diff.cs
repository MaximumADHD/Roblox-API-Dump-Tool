using System;
using System.Collections.Generic;

namespace RobloxApiDumpTool
{
    public enum DiffType
    {
        Add    = 1,
        Move   = 3,
        Merge  = 4,
        Change = 2,
        Remove = 5,
        Rename = 0,
    }

    public class Diff : IComparable
    {
        private const string NL = "\r\n";
        
        private readonly List<Diff> children = new List<Diff>();
        private int stack;

        public DiffType Type;

        public string Field = "";
        public object Context;

        public bool BiasTarget = false;
        public Descriptor Target;

        public DiffChangeList From = new DiffChangeList("ChangeFrom");
        public DiffChangeList To = new DiffChangeList("ChangeTo");

        public bool HasParent => (stack > 0);
        public Diff[] Children => children.ToArray();

        public bool Detailed;
        public bool Disposed;
        
        public void AddChild(Diff child)
        {
            if (!children.Contains(child))
            {
                child.stack++;
                children.Add(child);
            }
        }

        public void RemoveChild(Diff child)
        {
            if (children.Contains(child))
            {
                child.stack = 0;
                children.Remove(child);
            }
        }

        public string WriteDiffTxt(bool detailed = false)
        {
            string result = "";

            for (int i = 0; i < stack; i++)
                result += '\t';

            string what = Target.Describe(detailed);

            if (Type != DiffType.Change)
                what = (what.StartsWith(Field, StringComparison.InvariantCulture) ? "" : $"{Field} ") + what;
            else
                what = $"the {Field} of {what}";

            switch (Type)
            {
                case DiffType.Add:
                {
                    result += $"Added {what}";
                    break;
                }
                case DiffType.Change:
                {
                    string from = From.ToString();
                    string to = To.ToString();

                    string grouped = $"from {from} to {to}";
                    result += $"Changed {what}";

                    if (grouped.Length < 18)
                        result += $" {grouped}";
                    else
                        result += $" {NL}" +
                            $"\tfrom: {from}{NL}" +
                            $"\t  to: {to}{NL}";

                    break;
                }
                case DiffType.Remove:
                {
                    result += "Removed " + what;
                    break;
                }
                case DiffType.Rename:
                {
                    result += $"Renamed {Field}{NL}\"{Target.Name}\" to \"{To}\"";
                    break;
                }
                case DiffType.Merge:
                {
                    const string prefix = "\t• ";
                    string listed = From.ListElements(NL, prefix);

                    result +=
                        $"Merged: {NL}" +
                        $"{listed}{NL}" +
                        $"  into: {NL}" +
                        $"{prefix}{what}{NL}";

                    break;
                }
                case DiffType.Move:
                {
                    string descType = Target.DescriptorType;
                    string name = Target.Name;

                    string moveFrom = From.ToString();
                    string moveTo = To.ToString();

                    result += $"Moved {descType} {name}{NL}" +
                              $"\tfrom: {moveFrom}{NL}" +
                              $"\t  to: {moveTo}{NL}";

                    break;
                }  
            }

            if (children.Count > 0)
            {
                children.Sort();

                foreach (Diff child in children)
                {
                    result += NL;
                    result += child.WriteDiffTxt(detailed);
                }

                result += NL;
            }

            return result;
        }

        public override string ToString()
        {
            return WriteDiffTxt(Detailed);
        }

        public void WriteHtml(ReflectionHtml html)
        {
            string diffType = $"{Type}";

            if (Type == DiffType.Add)
                diffType += "e";

            diffType += "d";

            if (HasParent)
                diffType += " child";

            html.OpenDiv(diffType, () =>
            {
                switch (Type)
                {
                    case DiffType.Change:
                    {
                        // Check if we should keep this on one line, based on the text version.
                        string textSignature = WriteDiffTxt();
                        bool multiline = textSignature.Contains(NL);

                        // Write what we changed.
                        html.Span("WhatChanged", Field);

                        // Write what was changed.
                        Target.WriteHtml(html);

                        // Changed From, Changed To.
                        From.WriteHtml(html, multiline);
                        To.WriteHtml(html, multiline);

                        break;
                    }
                    case DiffType.Rename:
                    {
                        // Write what we're renaming.
                        html.OpenSpan(Field, () => html.Span("String", Target.Name));

                        // Write its new name.
                        To.WriteHtml(html);

                        break;
                    }
                    case DiffType.Merge:
                    {
                        // Write the elements that are being merged.
                        From.WriteHtml(html);

                        // Write what they merged into.
                        html.OpenSpan("MergeListInto", () => To.WriteHtml(html));

                        break;
                    }
                    case DiffType.Move:
                    {
                        string descType = Target.DescriptorType;
                        string name = $" {Target.Name}";

                        html.Span(descType, name);

                        From.WriteHtml(html, true);
                        To.WriteHtml(html, true);

                        break;
                    }
                    default:
                    {
                        string descType = Target.DescriptorType;
                        bool detailed = (Type == DiffType.Add);

                        if (Field != descType)
                        {
                            if (Context != null)
                            {
                                if (Context is Tags tags)
                                {
                                    string tagClass = "TagChange";

                                    if (tags.Count == 1)
                                        tagClass += " singular";

                                    if (Type == DiffType.Add)
                                        tagClass += " to";
                                    else
                                        tagClass += " from";

                                    html.OpenSpan(tagClass, () => tags.WriteHtml(html));
                                }
                                else if (Context is string legacyName)
                                {
                                    string nameClass = "LegacyName";

                                    if (Type == DiffType.Add)
                                        nameClass += " to";
                                    else
                                        nameClass += " from";

                                    html.OpenSpan(nameClass, () => html.Span("String", legacyName));
                                }

                                detailed = false;
                            }
                            else
                            {
                                html.Span("Field", Field);
                            }
                        }

                        html.OpenSpan("Target", () => Target.WriteHtml(html));
                        break;
                    }
                }

                if (children.Count > 0)
                {
                    children.Sort();
                    children.ForEach(child => child.WriteHtml(html));
                }
            });
        }

        public int CompareTo(object obj)
        {
            if (!(obj is Diff diff))
                throw new NotImplementedException("Diff can only be compared with another Diff");

            var diffTarget = diff.Target;

            if (Target == null || diffTarget == null)
                throw new NotSupportedException("Both Diffs must have their Target fields defined!");

            // Compare diff types.
            if (Type != diff.Type)
                return Type - diff.Type;

            // Compare type priorities.
            var myPriority = Target.TypePriority;
            var diffPriority = diffTarget.TypePriority;

            if (Field.StartsWith("Tag"))
                myPriority = TypePriority.Tag;
            else if (Field.StartsWith("Legacy"))
                myPriority = TypePriority.LegacyName;

            if (diff.Field.StartsWith("Tag"))
                diffPriority = TypePriority.Tag;
            else if (diff.Field.StartsWith("Legacy"))
                diffPriority = TypePriority.LegacyName;

            if (myPriority != diffPriority)
                return myPriority - diffPriority;
            
            // Compare fields.
            if (!BiasTarget)
            {
                int sortByField = Field.CompareTo(diff.Field);
                if (sortByField != 0) return sortByField;
            }
            
            // Compare targets.
            int sortByTarget = Target.CompareTo(diff.Target);

            if (sortByTarget != 0)
                return sortByTarget;

            // Compare fields.
            if (BiasTarget)
            {
                int sortByField = Field.CompareTo(diff.Field);
                if (sortByField != 0) return sortByField;
            }

            // These are identical?
            return 0;
        }
    }
}
