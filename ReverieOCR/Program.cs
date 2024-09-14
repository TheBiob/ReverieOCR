using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Reflection;
using System.Text;

const string MAPPING_FILE = @".\mapping.txt";

[DoesNotReturn]
static void error(string msg, params string[] args) {
    Console.WriteLine(msg, args);
    Console.ReadKey(true);
    Environment.Exit(1);
}

if (args.Length < 1) {
    error("Usage ReverieOCR.exe <path/to/image.png>");
}

var file = args[0];//@"D:\Games\SMW\Hacks\Reverie\section3\test\test.png";
if (!File.Exists(file)) {
    error("File '{0}' not found", file);
}

var image = Image.FromFile(file) as Bitmap;

if (image == null) {
    error("Could not read image");
}

var ocr = new ReverieOCR.ReverieOCR(image);
var glyphs = ocr.GetCharacters();

var drawLetters = true;
var font = "Arial";
var fontSize = 6f;
int? colEmpty = 0;
int? colSet = 0;
int? colEmptyGlyph = 0;
int? colSetGlyph = 0;

static int? ToColor(string input) {
    if (input.Equals("image", StringComparison.OrdinalIgnoreCase))
        return null;

    var col = Color.FromName(input);
    if (!col.IsKnownColor)
        col = Color.FromArgb(Convert.ToInt32(input, 16));

    return col.ToArgb();
}

var charmap = new Dictionary<int, string>();
var mapping_file = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, MAPPING_FILE);
if (File.Exists(mapping_file)) {
    var content = File.ReadAllLines(mapping_file);
    foreach (var line in content) {
        
        var split = line.Split(' ');

        if (split.Length < 2)
            continue;

        switch (split[0].ToLower()) {
            case "font":
                font = split[1];
                break;
            case "fontsize":
                fontSize = float.Parse(split[1]);
                break;
            case "color_0":
                colEmpty = ToColor(split[1]);
                break;
            case "color_1":
                colSet = ToColor(split[1]);
                break;
            case "color_glyph_0":
                colEmptyGlyph = ToColor(split[1]);
                break;
            case "color_glyph_1":
                colSetGlyph = ToColor(split[1]);
                break;
            case "drawletters":
                drawLetters = Convert.ToBoolean(split[1]);
                break;
            default:
                charmap.Add(Convert.ToInt32(split[0], 2), split[1]);
                break;
        }
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
        glyph.Mapped = str;
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

ocr.SaveImageData(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(file))!, Path.GetFileNameWithoutExtension(file)+"_output.png"),
    drawLetters ? glyphs : null, font, fontSize,
    colEmpty, colSet, colEmptyGlyph, colSetGlyph);

Console.ReadKey(true);
