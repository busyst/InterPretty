var terr = @"C:\Users\JD\Desktop\MKERSHARP\Generated\";
Interpreter InterpretThis(string path)
{
    Lexer lexer = new Lexer(File.ReadAllText(path));
    List<Token> tokens = [];
    while (true)
    {
        var token = lexer.Lex();
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






File.Delete($"{terr}bootloader.bin");
File.Delete($"{terr}kernel.bin");
File.Delete($"{terr}result.raw");

Interpreter.bootloader = true;
var bootloader = InterpretThis($"{terr}bootloader.ams");
Interpreter.bootloader = false;
var kernel = InterpretThis($"{terr}kernel.ams");
File.WriteAllText(terr+"bootloader.asm",bootloader.output);
File.WriteAllText(terr+"kernel.asm",kernel.output);
ConCom.Run(@$"nasm -f bin {terr}bootloader.asm -o {terr}bootloader.bin");  
ConCom.Run(@$"nasm -f bin {terr}kernel.asm -o {terr}kernel.bin");
if (!File.Exists($"{terr}bootloader.bin") || !File.Exists($"{terr}kernel.bin"))
{
    Console.WriteLine("Error: NASM compilation failed");
    return;
}
ConCom.Run(@$"copy {terr}bootloader.bin + {terr}kernel.bin {terr}result.raw");
//ConCom.Run(@"C:\Users\JD\Desktop\MKERSHARP\mkisofs.exe", @$"-o {terr}disk.iso -b {terr}result.raw -no-emul-boot {terr}result.raw");
ConCom.Run($@"qemu-system-x86_64 -drive file={terr}result.raw,format=raw");



File.Delete($"{terr}bootloader.bin");
File.Delete($"{terr}kernel.bin");
File.Delete($"{terr}result.raw");