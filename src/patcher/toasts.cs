using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;

// toasts <assembly...> : 列出所有 ToastManager.RegisterToast(name, title, description) 的字符串
class Toasts
{
    static string Esc(string s)
    {
        return s.Replace("\\", "\\\\").Replace("\t", "\\t").Replace("\r", "\\r").Replace("\n", "\\n");
    }

    static void Main(string[] args)
    {
        foreach (string path in args)
        {
            ModuleDefinition m;
            try { m = ModuleDefinition.ReadModule(path); } catch { continue; }
            string asm = Path.GetFileName(path);
            foreach (TypeDefinition t in m.GetTypes())
            {
                foreach (MethodDefinition md in t.Methods)
                {
                    if (!md.HasBody) continue;
                    List<Instruction> il = new List<Instruction>(md.Body.Instructions);
                    for (int i = 0; i < il.Count; i++)
                    {
                        MethodReference mr = il[i].Operand as MethodReference;
                        if (mr == null || mr.Name != "RegisterToast" || mr.DeclaringType.Name != "ToastManager") continue;
                        // 收集前三个 ldstr
                        List<string> vals = new List<string>();
                        for (int j = i - 1; j >= 0; j--)
                        {
                            if (il[j].OpCode.Code == Code.Ldstr) { vals.Insert(0, (string)il[j].Operand); continue; }
                            MethodReference prev = il[j].Operand as MethodReference;
                            if (prev != null && prev.Name.StartsWith("get_ToastManager")) break;
                            if (il[j].OpCode.Code == Code.Call || il[j].OpCode.Code == Code.Callvirt || il[j].OpCode.Code == Code.Newobj) break;
                            if (il[j].OpCode.Code == Code.Ret || il[j].OpCode.Code == Code.Stsfld) break;
                        }
                        if (vals.Count >= 2)
                            Console.WriteLine(asm + "\t" + t.FullName + "\t" + Esc(vals[vals.Count - 2]) + "\t" + Esc(vals[vals.Count - 1]));
                    }
                }
            }
        }
    }
}
