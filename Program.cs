using System.Diagnostics;
var terr = @".\Generated\";
var code = @".\Code\";

Bootloader bootloader = new Bootloader($"{code}bootloader.ams",$"{terr}bootloader.asm",$"{terr}bootloader.bin");
// /bootloader.Interpret();
;
Kernel kernel = new Kernel($"{code}kernel.ams",$"{terr}kernel.asm",$"{terr}kernel.bin");
kernel.Interpret();
;

if (bootloader.Compile() && kernel.Compile())
{
    OS os = new(bootloader,kernel,$"{terr}os.raw");
    os.CompileRun();
    os.Dispose();
}
else
{
    System.Console.WriteLine("Mods, crush his scull");
    bootloader.DeleteBin();
    kernel.DeleteBin();
}
//ConCom.Run(@"C:\Users\JD\Desktop\MKERSHARP\mkisofs.exe", @$"-o {terr}disk.iso -b {terr}result.raw -no-emul-boot {terr}result.raw");

class OS(Bootloader bootloader,Kernel kernel,string rawFile) : IDisposable
{
    public void CompileRun()
    {
        try
        {
            File.Delete(rawFile);
        }catch{
            foreach (var process in Process.GetProcessesByName("qemu-system-x86_64"))
            {
                process.Kill();
            }
            File.Delete(rawFile);
        }
        using var os = File.Create(rawFile);
        os.Write(File.ReadAllBytes(bootloader.pathToBin));
        os.Write(File.ReadAllBytes(kernel.pathToBin));
        os.Dispose();

        FileInfo fileInfo = new FileInfo(rawFile);
        FileInfo BootInfo = new FileInfo(bootloader.pathToBin);
        FileInfo KernelInfo = new FileInfo(kernel.pathToBin);
        
        Console.WriteLine($"Kernel size:{KernelInfo.Length}\r\nBootloader size:{BootInfo.Length}\r\nOs size:{fileInfo.Length}");
       
        byte[] bytes = new byte[1024];

        byte[] buffer = new byte[512];
        File.ReadAllBytes(bootloader.pathToBin).CopyTo  (buffer,0);
        buffer[510] = 0x55;
        buffer[511] = 0xAA;
        buffer.CopyTo(bytes,0);
        var s = File.ReadAllBytes(kernel.pathToBin);

        s.CopyTo(bytes,buffer.Length);
        File.WriteAllBytes(rawFile,bytes);

        ConCom.Run($@"qemu-system-x86_64 -drive file={rawFile},format=raw");
    }

    public void Dispose()
    {
        File.Delete(rawFile);
        bootloader.DeleteBin();
        kernel.DeleteBin();
    }
}
public abstract class CompiledCode
{
    public string pathToCode,pathToAsm,pathToBin;

    protected CompiledCode(string pathToCode,string pathToAsm,string pathToBin)
    {
        this.pathToBin = pathToBin;
        this.pathToCode = pathToCode;
        this.pathToAsm = pathToAsm;
    }

    public virtual bool Compile()
    {
        try
        {
            ConCom.Run(@$"nasm -f bin {pathToAsm} -o {pathToBin}");
            File.ReadAllBytes(pathToBin);
            return true;
        }
        catch
        {
            File.Delete(pathToBin);
            return false;
        }
    }
    public virtual void Interpret()
    {
        Lexer lexer = new(File.ReadAllText(pathToCode));
        List<Token> tokens = [];
        while (true)
        {
            var token = lexer.Lex();
            if(token==null)
                continue;
            if(token.type == TokenType.EOF_TOKEN)
                break;
            tokens.Add(token);
        }
        ParserV3 p = new ParserV3(tokens);
        p.Parse();

        Interpreter interpreter = new Interpreter(p.statements);
        var t = interpreter.Interpret();
        File.WriteAllText(pathToAsm,t);
    }
    public virtual void DeleteBin()
    {
        File.Delete(pathToBin);
    }
}
public class Bootloader : CompiledCode
{
    public Bootloader(string pathToCode, string pathToBin, string pathToAsm) : base(pathToCode, pathToBin, pathToAsm)
    {
    }
    public override void Interpret()
    {
        Interpreter.bootloader = true;
        base.Interpret();
    }
}
public class Kernel : CompiledCode
{
    public Kernel(string pathToCode, string pathToBin, string pathToAsm) : base(pathToCode, pathToBin, pathToAsm)
    {
    }
    public override void Interpret()
    {
        Interpreter.bootloader = false;
        base.Interpret();
    }
}