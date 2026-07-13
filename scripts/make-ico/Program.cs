using System.Drawing;
using System.Runtime.InteropServices;

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: MakeIco <input.png> <output.ico>");
    return 1;
}

var input = args[0];
var output = args[1];

using var source = new Bitmap(input);
using var icon32 = new Bitmap(source, new Size(32, 32));
var handle = icon32.GetHicon();
try
{
    using var icon = Icon.FromHandle(handle);
    using var stream = File.Create(output);
    icon.Save(stream);
}
finally
{
    DestroyIcon(handle);
}

Console.WriteLine($"Wrote {output} ({new FileInfo(output).Length} bytes)");
return 0;

[DllImport("user32.dll", CharSet = CharSet.Auto)]
static extern bool DestroyIcon(IntPtr handle);
