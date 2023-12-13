using System.Diagnostics;

static class ConCom
{
    public static void Run(string command)
    {
        // Set up the process start info
        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = "cmd.exe",          // Use the command prompt
            RedirectStandardInput = true,  // Redirect standard input
            RedirectStandardOutput = true, // Redirect standard output
            RedirectStandardError = true,  // Redirect standard error
            CreateNoWindow = true,         // Do not create a window
            UseShellExecute = false        // Do not use the OS shell
        };

        // Start the process
        using Process process = new() { StartInfo = psi };
        process.Start();

        // Pass the command to the command prompt
        process.StandardInput.WriteLine(command);
        process.StandardInput.WriteLine("exit"); // exit the command prompt

        string errors = process.StandardError.ReadToEnd();
        if(errors.Length!=0)
        {
            System.Console.WriteLine(errors);
        }


        process.WaitForExit();
    }
    public static void Run(string path,string arguments)
    {
        // Set up the process start info
        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = path,          // Use the command prompt
            RedirectStandardInput = true,  // Redirect standard input
            RedirectStandardOutput = true, // Redirect standard output
            RedirectStandardError = true,  // Redirect standard error
            CreateNoWindow = true,         // Do not create a window
            UseShellExecute = false,        // Do not use the OS shell
            Arguments = arguments,
        };

        // Start the process
        using Process process = new() { StartInfo = psi };
        process.Start();


        string errors = process.StandardError.ReadToEnd();
        if(errors.Length!=0)
        {
            System.Console.WriteLine(errors);
        }


        process.WaitForExit();
    }
}