using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

[DllImport("user32.dll")]
static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey,byte bAlpha, uint dwFlags);
[DllImport("user32.dll")]
static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
[DllImport("user32.dll")]
static extern int GetWindowLong(IntPtr window, int index);
[DllImport("user32.dll")]
static extern bool EnumThreadWindows(int dwThreadId, EnumThreadDelegate lpfn, IntPtr lParam);

const int GWL_EXSTYLE = -20;
const int WS_EX_LAYERED = 0x80000;
const int LWA_ALPHA = 0x2;
const int LWA_COLORKEY = 0x1;

static Tuple<string, byte> ReadArguments(string[] args)
{
    string processName;
    string opacity;
    
    if (args.Length != 2)
    {
        Console.Write("Process name: ");
        processName = Console.ReadLine() ?? "";
    
        Console.Write("Opacity value [0-100]: ");
        opacity = Console.ReadLine() ?? "100";
    }
    else
    {
        processName = args[0];
        opacity = args[1];
    }

    if (string.IsNullOrWhiteSpace(processName))
        throw new Exception("Process name is empty or is a whitespace");

    if (!byte.TryParse(opacity, out var opacityByte))
        throw new Exception("Invalid opacity value");

    if (opacityByte > 100)
        throw new Exception("Opacity value can not be greater than 100");

    opacityByte = (byte)(opacityByte * 255 / 100);
    
    return new Tuple<string, byte>(processName, opacityByte);
}

static int GetFirstProcessByName(string processName)
{
    var processList = Process.GetProcessesByName(processName);
    return processList.Any() ? processList.First().Id : -1;
}

static IEnumerable<IntPtr> EnumerateProcessWindowHandles(int processId)
{
    if (processId == -1)
    {
        return Enumerable.Empty<IntPtr>();
    }
    
    var handles = new List<IntPtr>();

    foreach (ProcessThread thread in Process.GetProcessById(processId).Threads)
    {
        EnumThreadWindows(thread.Id,
            (hWnd, lParam) =>
            {
                handles.Add(hWnd);
                return true;
            }, IntPtr.Zero);
    }

    return handles;
}

static void SetOpacity(IEnumerable<IntPtr> windowHandles, byte opacity)
{
    foreach (var handle in windowHandles)
    {
        SetWindowLong(handle, GWL_EXSTYLE, (int) (GetWindowLong(handle, GWL_EXSTYLE) ^ (nint) WS_EX_LAYERED));
        SetLayeredWindowAttributes(handle, 0, opacity, LWA_ALPHA);
    }
}

try
{
    var arguments = ReadArguments(args);
    var processId = GetFirstProcessByName(arguments.Item1);
    var handles = EnumerateProcessWindowHandles(processId);
    SetOpacity(handles, arguments.Item2);
}
catch (Exception e)
{
    Console.WriteLine($"Error: {e.Message}");
}

delegate bool EnumThreadDelegate(IntPtr hWnd, IntPtr lParam);