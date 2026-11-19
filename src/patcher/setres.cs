using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;

// 用法:
//   getres <assembly> <outdir>            导出所有内嵌文本类资源（.cfg/.txt/.shader 等）
//   getres <assembly> <resourceName>      仅导出指定资源到 stdout 目录
//   setres <assembly> <resourceName> <file>   用文件内容替换内嵌资源
class ResTool
{
    static byte[] ReadAll(Stream s)
    {
        MemoryStream ms = new MemoryStream();
        byte[] buf = new byte[16384];
        int n;
        while ((n = s.Read(buf, 0, buf.Length)) > 0) ms.Write(buf, 0, n);
        return ms.ToArray();
    }

    static void Main(string[] args)
    {
        if (args[0] == "utf8")
        {
            // 把指定方法里读取资源用的 Encoding.ASCII 换成 Encoding.UTF8
            ModuleDefinition m = ModuleDefinition.ReadModule(args[1]);
            string typeFilter = args[2];
            string methodFilter = args[3];
            MethodReference utf8ref = m.ImportReference(typeof(Encoding).GetProperty("UTF8").GetGetMethod());
            int hits = 0;
            foreach (TypeDefinition t in m.GetTypes())
            {
                if (!t.FullName.EndsWith(typeFilter)) continue;
                foreach (MethodDefinition md in t.Methods)
                {
                    if (md.Name != methodFilter || !md.HasBody) continue;
                    foreach (Instruction ins in md.Body.Instructions)
                    {
                        MethodReference mr = ins.Operand as MethodReference;
                        if (mr == null) continue;
                        if (mr.Name == "get_ASCII" && mr.DeclaringType.Name == "Encoding")
                        {
                            ins.Operand = utf8ref;
                            hits++;
                        }
                    }
                }
            }
            if (hits == 0) { Console.WriteLine("already UTF-8: " + Path.GetFileName(args[1])); return; }
            string tmp2 = args[1] + ".new";
            m.Write(tmp2);
            m.Dispose();
            File.Copy(tmp2, args[1], true);
            File.Delete(tmp2);
            Console.WriteLine("patched " + hits + " ASCII->UTF8 call(s) in " + Path.GetFileName(args[1]));
            return;
        }

        if (args.Length < 3) { Console.WriteLine("usage: getres <asm> <outdir|name> | setres <asm> <name> <file> | utf8 <asm> <typeSuffix> <method>"); return; }

        if (args[0] == "getres")
        {
            ModuleDefinition m = ModuleDefinition.ReadModule(args[1]);
            string target = args[2];
            bool single = Directory.Exists(target) == false;
            if (!single) Directory.CreateDirectory(target);
            foreach (Resource r in m.Resources)
            {
                EmbeddedResource er = r as EmbeddedResource;
                if (er == null) continue;
                if (single && er.Name != target) continue;
                byte[] data;
                using (Stream s = er.GetResourceStream()) data = ReadAll(s);
                if (single)
                {
                    File.WriteAllBytes(Path.GetFileName(er.Name), data);
                    Console.WriteLine("wrote " + Path.GetFileName(er.Name) + " (" + data.Length + " bytes)");
                }
                else
                {
                    bool text = !er.Name.ToLower().EndsWith(".png") && !er.Name.ToLower().EndsWith(".md3");
                    string fn = Path.Combine(target, er.Name.Replace('/', '_'));
                    if (text) fn += ".txt";
                    File.WriteAllBytes(fn, data);
                    Console.WriteLine(er.Name + " -> " + fn);
                }
            }
            return;
        }

        if (args[0] == "setres")
        {
            ModuleDefinition m = ModuleDefinition.ReadModule(args[1]);
            byte[] data = File.ReadAllBytes(args[3]);
            for (int i = 0; i < m.Resources.Count; i++)
            {
                EmbeddedResource er = m.Resources[i] as EmbeddedResource;
                if (er == null || er.Name != args[2]) continue;
                m.Resources[i] = new EmbeddedResource(er.Name, er.Attributes, data);
                string tmp = args[1] + ".new";
                m.Write(tmp);
                m.Dispose();
                File.Copy(tmp, args[1], true);
                File.Delete(tmp);
                Console.WriteLine("replaced " + er.Name + " in " + Path.GetFileName(args[1]) + " (" + data.Length + " bytes)");
                return;
            }
            Console.WriteLine("ERROR: resource not found: " + args[2]);
        }
    }
}
