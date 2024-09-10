using System;
using System.Security.Cryptography;

namespace ReverieOCR;

public class Glyph
{
    public const int GLYPH_SIZE = 6;

    public bool[] Encoded {get;set;} = new bool[8];
    public bool IsValid {get;set;} = true;

    public int X {get;set;}
    public int Y {get;set;}

    private static (Range xrange, Range yrange)[] offsets = [(1..1, 0..0), (5..5, 1..1), (4..4, 5..5), (0..0, 4..4), (2..3, 1..1), (4..4, 2..3), (2..3, 4..4), (1..1, 2..3)];

    public int GetNumericValue()
    {
        var result = 0;
        for (var i = 0; i < Encoded.Length; i++)
        {
            result |= Encoded[i]?1<<(Encoded.Length-i-1):0;
        }
        return result;
    }

    public static Glyph FromImageData(ReverieOCR data, int xstart, int ystart)
    {
        var glyph = new Glyph() {
            X = xstart,
            Y = ystart,
        };

        for (var i = 0; i < offsets.Length; i++)
        {
            var result = false;
            var (xrange, yrange) = offsets[i];
            // if any pixel within the range is set, assume "true"
            for (var x = xstart+xrange.Start.Value; x <= xstart+xrange.End.Value && !result; x++)
            {
                for (var y = ystart+yrange.Start.Value; y <= ystart+yrange.End.Value && !result; y++)
                {
                    result |= data.GetPixel(x, y);
                }
            }

            glyph.Encoded[i] = result;
        }

        // TODO validate;

        return glyph;
    }

    internal void MarkUsed(ReverieOCR data)
    {
        for (var y = Y; y < Y+GLYPH_SIZE; y++) {
            for (var x = X; x < X+GLYPH_SIZE; x++) {
                data.MarkUsed(x, y);
            }
        }
    }
}
