using System;
using System.Security.Cryptography;

namespace ReverieOCR;

public class Glyph
{
    public const int GLYPH_SIZE = 6;

    public string Mapped {get;set;} = "-";
    public bool[] Encoded {get;set;} = new bool[8];
    public bool IsValid {get;set;} = true;

    public int X {get;set;}
    public int Y {get;set;}

    private static (Range xrange, Range yrange)[] offsets = [(1..1, 0..0), (5..5, 1..1), (4..4, 5..5), (0..0, 4..4), (2..3, 1..1), (4..4, 2..3), (2..3, 4..4), (1..1, 2..3)];
    private static (Range xrange, Range yrange, int minSet)[] line_validation_ranges = [(1..4, 0..0, 4), (5..5, 1..4, 4), (1..4, 5..5, 4), (0..0, 1..4, 4), (2..3, 1..1, 1), (4..4, 2..3, 1), (2..3, 4..4, 1), (1..1, 2..3, 1),
                                                                                        (0..0, 0..0, 0), (5..5, 0..0, 0), (5..5, 5..5, 0), (0..0, 5..5, 0), (1..1, 1..1, 0), (4..4, 1..1, 0), (4..4, 4..4, 0), (1..1, 4..4, 0)];

    private void Validate(IGetPixel data)
    {
        IsValid = true;

        for (var i = 0; i < line_validation_ranges.Length; i++) {
            var (xrange, yrange, minSet) = line_validation_ranges[i];

            var total = 0;
            for (var x = X+xrange.Start.Value; x <= X+xrange.End.Value; x++)
            {
                for (var y = Y+yrange.Start.Value; y <= Y+yrange.End.Value; y++)
                {
                    if (data.GetPixel(x, y)) {
                        total += 1;
                    }
                }
            }

            if (i < Encoded.Length) {
                if (Encoded[i] && total < minSet || !Encoded[i] && total != 0) {
                    IsValid = false;
                    break;
                }
            } else if (total != 0) {
                IsValid = false;
                break;
            }
        }
    }

    public int GetNumericValue()
    {
        var result = 0;
        for (var i = 0; i < Encoded.Length; i++)
        {
            result |= Encoded[i]?1<<(Encoded.Length-i-1):0;
        }
        return result;
    }

    public static Glyph FromImageData(IGetPixel data, int xstart, int ystart)
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

        glyph.Validate(data);

        return glyph;
    }

    internal void MarkUsed(IGetPixel data)
    {
        for (var y = Y; y < Y+GLYPH_SIZE; y++) {
            for (var x = X; x < X+GLYPH_SIZE; x++) {
                data.MarkUsed(x, y);
            }
        }
    }
}
