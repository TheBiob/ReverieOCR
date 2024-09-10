using System.Drawing;
using System.Reflection;
using System.Text;

const string MAPPING_FILE = @".\mapping.txt";

if (args.Length < 1) {
    Console.WriteLine("Usage ReverieOCR.exe <path/to/image.png>");
    Environment.Exit(1);
}

var file = args[0];//@"D:\Games\SMW\Emulators\snes9x-1.60\Screenshots\Reverie000.png";
if (!File.Exists(file)) {
    Console.WriteLine("File '{0}' not found", file);
    Environment.Exit(1);
}

var image = Image.FromFile(file) as Bitmap;

if (image == null) {
    Console.WriteLine("Could not read image");
    Environment.Exit(1);
}

var ocr = new ReverieOCR.ReverieOCR(image);
var glyphs = ocr.GetCharacters();

var charmap = new Dictionary<int, string>();
var mapping_file = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, MAPPING_FILE);
if (File.Exists(mapping_file)) {
    var content = File.ReadAllLines(mapping_file);
    foreach (var line in content) {
        if (string.IsNullOrWhiteSpace(line))
            continue;
        
        var split = line.Split(' ');
        charmap.Add(Convert.ToInt32(split[0], 2), split[1]);
    }
} else {
    Console.WriteLine("Couldn't find " + mapping_file);
}

var lasty = -1;
var outputstr = new StringBuilder(Path.GetFileName(file)+"\r\n\r\n");
foreach (var glyph in glyphs)
{
    if (glyph.Y != lasty) {
        lasty = glyph.Y;
        outputstr.AppendLine();
    }
    if (charmap.TryGetValue(glyph.GetNumericValue(), out var str))
    {
        outputstr.Append(" "+str+" ");
    }
    else
    {
        Console.WriteLine("Unknown Character {0}", Convert.ToString(glyph.GetNumericValue(), 2).PadLeft(8, '0'));
    }
}

var result = outputstr.ToString().Trim();
Console.WriteLine(result);
File.WriteAllText(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(file))!, Path.GetFileNameWithoutExtension(file)+".txt"), result);

ocr.SaveImageData(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(file))!, Path.GetFileNameWithoutExtension(file)+"_data.png"));

Console.ReadKey(true);
