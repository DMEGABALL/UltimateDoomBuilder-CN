using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;

// 开发辅助工具：导出所有字符串及其使用上下文，便于筛查未翻译的界面文本。
// 用法: scan <out.tsv> <assembly> [assembly...]
class Scan
{
    static HashSet<string> AllowedProps = new HashSet<string>(new string[] {
        "set_Text", "set_Label", "set_Title", "set_HeaderText", "set_Header",
        "set_ToolTipText", "set_ToolTipTitle", "set_PlaceholderText", "set_ShortcutKeyDisplayString",
        "set_AccessibleDescription", "set_AccessibleName"
    });

    static string[] BlockedCallPrefixes = new string[] {
        "ResourceManager.", "ImageCollection.", "Configuration.", "ProgramConfiguration.",
        "UniFields.", "String.op_", "String.Equals", "Dictionary`2.", "HashSet`1.",
        "Regex.", "Path.", "Directory.", "File.", "Int32.", "Object.ToString", "Font.",
        "Enum.", "Convert.", "General.WriteLogLine", "Type.GetType"
    };

    static bool BlockedProp(string name)
    {
        return name == "set_Name" || name == "set_ImageKey" || name == "set_Filter";
    }

    static string ContextOf(Instruction ins)
    {
        Instruction cur = ins.Next;
        int guard = 0;
        while (cur != null && guard++ < 24)
        {
            Code c = cur.OpCode.Code;
            if (c == Code.Call || c == Code.Callvirt || c == Code.Newobj)
            {
                MethodReference mr = cur.Operand as MethodReference;
                if (mr == null) return "call?";
                if (mr.Name.StartsWith("set_") && mr.Parameters.Count == 1 && mr.Parameters[0].ParameterType.FullName == "System.String")
                    return BlockedProp(mr.Name) ? "SKIP" : "PROP:" + mr.Name;
                string key = mr.DeclaringType.Name + "." + mr.Name;
                if (key == "Font..ctor") return "FONT";
                foreach (string pre in BlockedCallPrefixes) if (key.StartsWith(pre)) return "SKIP";
                return "CALL:" + key;
            }
            if (c == Code.Ldstr || c == Code.Ret || c == Code.Br || c == Code.Brtrue || c == Code.Brfalse
                || c == Code.Brtrue_S || c == Code.Brfalse_S || c == Code.Switch || c == Code.Throw)
                return "FREE";
            if (c == Code.Stloc || c == Code.Stloc_0 || c == Code.Stloc_1 || c == Code.Stloc_2 || c == Code.Stloc_3
                || c == Code.Stfld || c == Code.Stsfld)
                return "STORE";
            cur = cur.Next;
        }
        return "FREE";
    }

    static string Clean(string s)
    {
        return s.Replace("\\", "\\\\").Replace("\t", "\\t").Replace("\r", "\\r").Replace("\n", "\\n");
    }

    static void Main(string[] args)
    {
        StreamWriter w = new StreamWriter(args[0], false, new UTF8Encoding(false));
        w.WriteLine("file\tctx\towner\ttext");
        for (int i = 1; i < args.Length; i++)
        {
            string path = Path.GetFullPath(args[i]);
            string name = Path.GetFileName(path);
            ModuleDefinition module = ModuleDefinition.ReadModule(path);
            foreach (TypeDefinition t in module.GetTypes())
            {
                foreach (MethodDefinition m in t.Methods)
                {
                    if (!m.HasBody) continue;
                    foreach (Instruction ins in m.Body.Instructions)
                    {
                        if (ins.OpCode.Code != Code.Ldstr) continue;
                        string v = ins.Operand as string;
                        if (v == null || v.Length == 0) continue;
                        w.WriteLine(name + "\t" + ContextOf(ins) + "\t" + t.FullName + "::" + m.Name + "\t" + Clean(v));
                    }
                }

                List<KeyValuePair<string, CustomAttribute>> attrs = new List<KeyValuePair<string, CustomAttribute>>();
                foreach (CustomAttribute ca in t.CustomAttributes) attrs.Add(new KeyValuePair<string, CustomAttribute>("type:" + t.Name, ca));
                foreach (FieldDefinition f in t.Fields) foreach (CustomAttribute ca in f.CustomAttributes) attrs.Add(new KeyValuePair<string, CustomAttribute>("field:" + f.Name, ca));
                foreach (PropertyDefinition p in t.Properties) foreach (CustomAttribute ca in p.CustomAttributes) attrs.Add(new KeyValuePair<string, CustomAttribute>("prop:" + p.Name, ca));
                foreach (MethodDefinition m in t.Methods) foreach (CustomAttribute ca in m.CustomAttributes) attrs.Add(new KeyValuePair<string, CustomAttribute>("method:" + m.Name, ca));
                foreach (KeyValuePair<string, CustomAttribute> kv in attrs)
                {
                    CustomAttribute ca = kv.Value;
                    string an = ca.AttributeType.Name;
                    for (int ai = 0; ai < ca.ConstructorArguments.Count; ai++)
                    {
                        string sv = ca.ConstructorArguments[ai].Value as string;
                        if (sv == null || sv.Length == 0) continue;
                        w.WriteLine(name + "\tATTR:" + an + ":#" + ai + "\t" + kv.Key + "\t" + Clean(sv));
                    }
                    for (int ni = 0; ni < ca.Properties.Count; ni++)
                    {
                        CustomAttributeNamedArgument na = ca.Properties[ni];
                        string sv = na.Argument.Value as string;
                        if (sv == null || sv.Length == 0) continue;
                        w.WriteLine(name + "\tATTR:" + an + ":" + na.Name + "\t" + kv.Key + "\t" + Clean(sv));
                    }
                }
            }
            module.Dispose();
        }
        w.Close();
        Console.WriteLine("done");
    }
}
