using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;

// 第二版提取器：为每个 ldstr 推断它的"用途上下文"
class Extract2
{
    static string Esc(string s)
    {
        if (s == null) return "";
        return s.Replace("\\", "\\\\").Replace("\t", "\\t").Replace("\r", "\\r").Replace("\n", "\\n");
    }

    static string ContextOf(MethodDefinition m, Instruction ins)
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
                    return "PROP:" + mr.Name;
                return "CALL:" + mr.DeclaringType.Name + "." + mr.Name;
            }
            if (c == Code.Ldstr || c == Code.Ret || c == Code.Br || c == Code.Brtrue || c == Code.Brfalse
                || c == Code.Brtrue_S || c == Code.Brfalse_S || c == Code.Switch || c == Code.Throw)
                return "NONE";
            if (c == Code.Stloc || c == Code.Stloc_0 || c == Code.Stloc_1 || c == Code.Stloc_2 || c == Code.Stloc_3
                || c == Code.Stfld || c == Code.Stsfld)
                return "STORE";
            cur = cur.Next;
        }
        return "NONE";
    }

    static string OwnerType(MethodDefinition m)
    {
        return m.DeclaringType.FullName;
    }

    static void Main(string[] args)
    {
        string outFile = args[0];
        using (StreamWriter w = new StreamWriter(outFile, false, new UTF8Encoding(false)))
        {
            for (int i = 1; i < args.Length; i++)
            {
                string path = args[i];
                string asm = Path.GetFileName(path);
                ModuleDefinition module;
                try { module = ModuleDefinition.ReadModule(path); }
                catch (Exception e) { Console.WriteLine("SKIP " + asm + ": " + e.Message); continue; }

                foreach (TypeDefinition t in module.Types)
                    Dump(w, asm, t);

                foreach (TypeDefinition t in module.Types)
                {
                    foreach (MethodDefinition md in t.Methods)
                        foreach (CustomAttribute ca in md.CustomAttributes) DumpAttr(w, asm, "method:" + t.FullName + "." + md.Name, ca);
                    foreach (FieldDefinition fd in t.Fields)
                        foreach (CustomAttribute ca in fd.CustomAttributes) DumpAttr(w, asm, "field:" + t.FullName + "." + fd.Name, ca);
                    foreach (PropertyDefinition pd in t.Properties)
                        foreach (CustomAttribute ca in pd.CustomAttributes) DumpAttr(w, asm, "prop:" + t.FullName + "." + pd.Name, ca);
                }
            }
        }
    }

    static void DumpAttr(TextWriter w, string asm, string owner, CustomAttribute ca)
    {
        string tn = ca.AttributeType.Name;
        if (tn != "DescriptionAttribute" && tn != "CategoryAttribute" && tn != "DisplayNameAttribute" && tn != "DefaultValueAttribute") return;
        foreach (CustomAttributeArgument a in ca.ConstructorArguments)
        {
            string sv = a.Value as string;
            if (sv != null) w.WriteLine(asm + "\t" + "ATTR:" + owner + "\t" + "ATTR:" + tn + "\t" + Esc(sv));
        }
        foreach (CustomAttributeNamedArgument na in ca.Properties)
        {
            string sv = na.Argument.Value as string;
            if (sv != null) w.WriteLine(asm + "\t" + "ATTR:" + owner + "\t" + "ATTR:" + tn + "|" + na.Name + "\t" + Esc(sv));
        }
    }

    static void Dump(TextWriter w, string asm, TypeDefinition t)
    {
        foreach (MethodDefinition m in t.Methods)
        {
            if (!m.HasBody) continue;
            foreach (Instruction ins in m.Body.Instructions)
            {
                if (ins.OpCode.Code != Code.Ldstr) continue;
                string v = ins.Operand as string;
                if (v == null) continue;
                string ctx = ContextOf(m, ins);
                w.WriteLine(asm + "\t" + OwnerType(m) + "\t" + ctx + "\t" + Esc(v));
            }
        }
        foreach (TypeDefinition n in t.NestedTypes) Dump(w, asm, n);
    }
}
