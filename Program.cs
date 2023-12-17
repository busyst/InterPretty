var terr = @"C:\Users\JD\Desktop\MKERSHARP\Generated\";


Bootloader bootloader = new Bootloader($"{terr}bootloader.ams",$"{terr}bootloader.asm",$"{terr}bootloader.bin");
bootloader.Interpret();
bootloader.Compile();
Kernel kernel = new Kernel($"{terr}kernel.ams",$"{terr}kernel.asm",$"{terr}kernel.bin");
kernel.Interpret();
kernel.Compile();

if (!File.Exists($"{terr}bootloader.bin") || !File.Exists($"{terr}kernel.bin"))
{
    Console.WriteLine("Error: NASM compilation failed");
    return;
}
OS os = new(bootloader,kernel,$"{terr}os.raw");
os.CompileRun();
os.Dispose();
//ConCom.Run(@"C:\Users\JD\Desktop\MKERSHARP\mkisofs.exe", @$"-o {terr}disk.iso -b {terr}result.raw -no-emul-boot {terr}result.raw");






class OS(Bootloader bootloader,Kernel kernel,string rawFile) : IDisposable
{
    public void CompileRun()
    {
        File.Delete(rawFile);
        using var os = File.Create(rawFile);
        os.Write(File.ReadAllBytes(bootloader.bootBIN));
        os.Write(File.ReadAllBytes(kernel.kernelBIN));
        os.Dispose();

        FileInfo fileInfo = new FileInfo(rawFile);
        FileInfo BootInfo = new FileInfo(bootloader.bootBIN);
        FileInfo KernelInfo = new FileInfo(kernel.kernelBIN);
        
        System.Console.WriteLine($"Kernel:{KernelInfo.Length}\r\nBootloader:{BootInfo.Length}\r\nOs size:{fileInfo.Length}");
       
        var s = File.ReadAllBytes(kernel.kernelBIN);
        byte[] bytes = new byte[(1024*8)-512];
        s.CopyTo(bytes,0);
        File.WriteAllBytes(kernel.kernelBIN,bytes);
        ConCom.Run($@"qemu-system-x86_64 -drive file={rawFile},format=raw");
    }

    public void Dispose()
    {
        File.Delete(rawFile);
        bootloader.Dispose();
        kernel.Dispose();
    }
}
class Bootloader(string input,string asmfile,string output) : IDisposable
{
    public string bootBIN =>output;
    public void Compile(){
        ConCom.Run(@$"nasm -f bin {asmfile} -o {output}");
        var s = File.ReadAllBytes(output);
        byte[] bytes = new byte[512];
        s.CopyTo(bytes,0);
        bytes[510] = 0x0055;
        bytes[511] = 0xAA;
        File.WriteAllBytes(output,bytes);
    }

    public void Dispose()
    {
        File.Delete(output);
    }

    public void Interpret(){
        Interpreter.bootloader = true;
        var bootloader = InterpretThis(input);
        File.WriteAllText(asmfile,bootloader.output);

    }
    Interpreter InterpretThis(string path)
    {
        Lexer lexer = new Lexer(File.ReadAllText(path));
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

        var main = new ParseFunction(){name = "main"};

        Parser parser = new Parser(tokens);
        parser.Parse(main);

        Interpreter interpreter = new Interpreter(main.ToFunc());
        interpreter.Interpret();
        return interpreter;
    }
}
class Kernel(string input,string asmfile,string output) : IDisposable
{
    public string kernelBIN =>output;
    public void Compile(){
        ConCom.Run(@$"nasm -f bin {asmfile} -o {output}");
        var s = File.ReadAllBytes(output);
    }

    public void Dispose()
    {
        File.Delete(output);
    }

    public void Interpret(){
        Interpreter.bootloader = false;
        var bootloader = InterpretThis(input);
        File.WriteAllText(asmfile,bootloader.output);

    }
    Interpreter InterpretThis(string path)
    {
        Lexer lexer = new Lexer(File.ReadAllText(path));
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

        var main = new ParseFunction(){name = "main"};

        Parser parser = new Parser(tokens);
        parser.Parse(main);

        Interpreter interpreter = new Interpreter(main.ToFunc());
        interpreter.Interpret();
        return interpreter;
    }
}