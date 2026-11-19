using System;
using System.Collections.Generic;
using System.IO;
using System.Resources;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;

class Apply
{
    static HashSet<string> AllowedProps = new HashSet<string>(new string[] {
        "set_Text", "set_Label", "set_Title", "set_HeaderText", "set_Header",
        "set_ToolTipText", "set_ToolTipTitle", "set_PlaceholderText", "set_ShortcutKeyDisplayString",
        "set_AccessibleDescription", "set_AccessibleName"
    });

    // 绝不翻译的上下文（内部键名、资源键、比较常量、路径等）
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
                    return BlockedProp(mr.Name) ? "SKIP" : "OK:PROP:" + mr.Name;
                string key = mr.DeclaringType.Name + "." + mr.Name;
                if (key == "Font..ctor") return "FONT";
                foreach (string pre in BlockedCallPrefixes) if (key.StartsWith(pre)) return "SKIP";
                return "OK:CALL:" + key;
            }
            if (c == Code.Ldstr || c == Code.Ret || c == Code.Br || c == Code.Brtrue || c == Code.Brfalse
                || c == Code.Brtrue_S || c == Code.Brfalse_S || c == Code.Switch || c == Code.Throw)
                return "OK";
            if (c == Code.Stloc || c == Code.Stloc_0 || c == Code.Stloc_1 || c == Code.Stloc_2 || c == Code.Stloc_3
                || c == Code.Stfld || c == Code.Stsfld)
                return "OK";
            cur = cur.Next;
        }
        return "OK";
    }

    static string Unesc(string s)
    {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < s.Length; i++)
        {
            if (s[i] == '\\' && i + 1 < s.Length)
            {
                char n = s[i + 1];
                if (n == '\\') { sb.Append('\\'); i++; continue; }
                if (n == 't') { sb.Append('\t'); i++; continue; }
                if (n == 'r') { sb.Append('\r'); i++; continue; }
                if (n == 'n') { sb.Append('\n'); i++; continue; }
            }
            sb.Append(s[i]);
        }
        return sb.ToString();
    }

    static Dictionary<string, string> Load(string path)
    {
        Dictionary<string, string> d = new Dictionary<string, string>();
        foreach (string line in File.ReadAllLines(path, Encoding.UTF8))
        {
            if (line.Length == 0 || line[0] == '#') continue;
            string[] p = line.Split('\t');
            if (p.Length < 2) continue;
            string en = Unesc(p[0]);
            string zh = Unesc(p[1]);
            if (en.Length == 0 || zh.Length == 0) continue;
            if (!d.ContainsKey(en)) d.Add(en, zh);
        }
        return d;
    }

    static void Main(string[] args)
    {
        Dictionary<string, string> dict = Load(args[0]);

        // 可选：无视上下文强制替换的表（用于撤销/重做描述等拼接片段）
        Dictionary<string, string> force = null;
        string forcePath = Environment.GetEnvironmentVariable("UDB_FORCE_DICT");
        if (!string.IsNullOrEmpty(forcePath) && File.Exists(forcePath))
        {
            force = Load(forcePath);
            Console.WriteLine("force entries: " + force.Count);
        }

        string outDir = args[1];
        Directory.CreateDirectory(outDir);
        int totalHits = 0;
        Console.WriteLine("dictionary entries: " + dict.Count);

        for (int i = 2; i < args.Length; i++)
        {
            string path = Path.GetFullPath(args[i]);
            string name = Path.GetFileName(path);
            string dest = Path.Combine(outDir, name);
            ModuleDefinition module = ModuleDefinition.ReadModule(path);
            int hits = 0;

            foreach (TypeDefinition t in module.GetTypes())
            {
                foreach (MethodDefinition m in t.Methods)
                {
                    if (!m.HasBody) continue;
                    foreach (Instruction ins in m.Body.Instructions)
                    {
                        if (ins.OpCode.Code != Code.Ldstr) continue;
                        string v = ins.Operand as string;
                        if (v == null) continue;
                        string zh = null;
                        bool forced = false;
                        if (force != null) forced = force.TryGetValue(v, out zh);
                        if (!forced)
                        {
                            string ctx = ContextOf(ins);
                            if (ctx == "SKIP") continue;
                            if (!dict.TryGetValue(v, out zh))
                            {
                                if (ctx == "FONT" && v == "Microsoft Sans Serif") zh = "Microsoft YaHei UI";
                                else continue;
                            }
                        }
                        if (zh == v) continue;
                        ins.Operand = zh;
                        hits++;
                    }
                }

                List<CustomAttribute> attrs = new List<CustomAttribute>();
                attrs.AddRange(t.CustomAttributes);
                foreach (FieldDefinition f in t.Fields) attrs.AddRange(f.CustomAttributes);
                foreach (PropertyDefinition p in t.Properties) attrs.AddRange(p.CustomAttributes);
                foreach (MethodDefinition m in t.Methods) attrs.AddRange(m.CustomAttributes);
                foreach (CustomAttribute ca in attrs)
                {
                    string tn = ca.AttributeType.Name;
                    // .NET 内置的显示用特性：所有字符串参数都可翻译
                    bool wholeAttribute = (tn == "DescriptionAttribute" || tn == "CategoryAttribute" || tn == "DisplayNameAttribute");
                    // 首个构造参数即显示名称的自定义特性：[ErrorChecker("...")] / [FindReplace("...")]
                    bool firstArgIsName = (tn == "ErrorCheckerAttribute" || tn == "FindReplaceAttribute");
                    // 项目自定义特性（如 [EditMode(DisplayName = "...")]）：只翻译显示用命名参数
                    for (int ai = 0; ai < ca.ConstructorArguments.Count; ai++)
                    {
                        if (!wholeAttribute && !(firstArgIsName && ai == 0)) continue;
                        string sv = ca.ConstructorArguments[ai].Value as string;
                        string zh;
                        if (sv == null || !dict.TryGetValue(sv, out zh)) continue;
                        if (zh == sv) continue;
                        ca.ConstructorArguments[ai] = new CustomAttributeArgument(ca.ConstructorArguments[ai].Type, zh);
                        hits++;
                    }
                    for (int ni = 0; ni < ca.Properties.Count; ni++)
                    {
                        CustomAttributeNamedArgument na = ca.Properties[ni];
                        if (!wholeAttribute && na.Name != "DisplayName" && na.Name != "Description") continue;
                        string sv = na.Argument.Value as string;
                        string zh;
                        if (sv == null || !dict.TryGetValue(sv, out zh)) continue;
                        if (zh == sv) continue;
                        ca.Properties[ni] = new CustomAttributeNamedArgument(na.Name, new CustomAttributeArgument(na.Argument.Type, zh));
                        hits++;
                    }
                }
            }

            // 嵌入资源中的字符串（少量）
            for (int ri = 0; ri < module.Resources.Count; ri++)
            {
                EmbeddedResource er = module.Resources[ri] as EmbeddedResource;
                if (er == null) continue;
                byte[] original;
                using (Stream s = er.GetResourceStream()) { original = ReadAll(s); }
                byte[] patched;
                int rhits;
                if (TryPatchResources(original, dict, out patched, out rhits))
                {
                    EmbeddedResource nr = new EmbeddedResource(er.Name, er.Attributes, patched);
                    module.Resources[ri] = nr;
                    hits += rhits;
                }
            }

            string tmp = dest + ".new";
            module.Write(tmp);
            module.Dispose();
            File.Copy(tmp, dest, true);
            File.Delete(tmp);
            totalHits += hits;
            Console.WriteLine(name + ": " + hits + " strings translated");
        }
        Console.WriteLine("total: " + totalHits);
    }

    static byte[] ReadAll(Stream s)
    {
        MemoryStream ms = new MemoryStream();
        byte[] buf = new byte[8192];
        int n;
        while ((n = s.Read(buf, 0, buf.Length)) > 0) ms.Write(buf, 0, n);
        return ms.ToArray();
    }

    static bool TryPatchResources(byte[] data, Dictionary<string, string> dict, out byte[] patched, out int hits)
    {
        patched = null;
        hits = 0;
        try
        {
            Dictionary<object, object> items = new Dictionary<object, object>();
            List<object> order = new List<object>();
            using (MemoryStream ms = new MemoryStream(data))
            {
                ResourceReader rr = new ResourceReader(ms);
                System.Collections.IDictionaryEnumerator en = rr.GetEnumerator();
                while (en.MoveNext())
                {
                    items[en.Key] = en.Value;
                    order.Add(en.Key);
                }
                rr.Close();
            }
            foreach (object k in order)
            {
                string v = items[k] as string;
                string zh;
                if (v != null && dict.TryGetValue(v, out zh) && zh != v) { items[k] = zh; hits++; }
            }
            if (hits == 0) return false;
            MemoryStream outMs = new MemoryStream();
            ResourceWriter rw = new ResourceWriter(outMs);
            foreach (object k in order) rw.AddResource((string)k, items[k]);
            rw.Close();
            patched = outMs.ToArray();
            return true;
        }
        catch (Exception) { return false; }
    }
}
