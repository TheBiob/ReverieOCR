using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace ReverieOCR;

public class ReverieOCR
{
    public enum PixelInfo {
        EMPTY = 0,
        SET = 1,
        EMPTY_PART_OF_GLYPH = 2,
        SET_PART_OF_GLYPH = 3,
    }

    public int ImageWidth {get;private set;}
    public int ImageHeight {get;private set;}
    private Bitmap Bitmap {get;set;}
    private  PixelInfo[] ImageData {get;set;}

    public ReverieOCR(Bitmap image)
    {
        ImageWidth = image.Width;
        ImageHeight = image.Height;
        Bitmap = image;
        ImageData = GetImageData(image);
    }

    private static PixelInfo[] GetImageData(Bitmap image)
    {
        var data = new int[image.Width*image.Height];

        BitmapData imageData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        Marshal.Copy(imageData.Scan0, data, 0, data.Length);
        image.UnlockBits(imageData);

        return ConvertToImageData(data, image.Width, image.Height);
    } 

    private static PixelInfo[] ConvertToImageData(int[] pixeldata, int _width, int _height) {
        return pixeldata.Select(x => (x&0xFFFFFF) > 0x800000 ? PixelInfo.SET : PixelInfo.EMPTY).ToArray();
    }

    public bool GetPixel(int x, int y) {
        if (y < 0 || y >= ImageHeight || x < 0 || x >= ImageWidth) {
            return false;
        }
        return ImageData[y*ImageWidth+x] == PixelInfo.SET;
    }

    internal void MarkUsed(int x, int y)
    {
        ImageData[y*ImageWidth+x] = ImageData[y*ImageWidth+x] == PixelInfo.SET ? PixelInfo.SET_PART_OF_GLYPH : PixelInfo.EMPTY_PART_OF_GLYPH;
    }

    internal Glyph[] GetCharacters()
    {
        var glyphs = new List<Glyph>();

        for (var y = 0; y < ImageHeight; y++) {
            for (var x = 0; x < ImageWidth; x++) {
                if (GetPixel(x, y)) {
                    var left = x == 0 ? false : GetPixel(x-1, y);
                    var top = y == 0 ? false : GetPixel(x, y-1);
                    var right = x == ImageWidth-1 ? false : GetPixel(x+1, y);
                    var bottom = y == ImageHeight-1 ? false : GetPixel(x, y+1);

                    // we are not at the top of the glyph, just continue
                    if (left || top) continue;

                    // we are at a corner, just continue
                    if (right && bottom) continue;

                    if (!right && !bottom) continue;

                    // if it can't fit, continue
                    if (right && x >= ImageWidth-Glyph.GLYPH_SIZE) continue;
                    if (bottom && y >= ImageHeight-Glyph.GLYPH_SIZE) continue;

                    var xdirection = right ? 1 : 0;
                    var ydirection = right ? 0 : 1;

                    var length = 1;
                    for (; length < Glyph.GLYPH_SIZE; length++) { // todo: might crash at edge
                        if (!GetPixel(x+xdirection*length, y+ydirection*length)) {
                            break;
                        }
                    }

                    if (length != 4) continue;

                    var glyphx = x-xdirection;
                    var glyphy = y-ydirection;

                    // if it can't fit, continue
                    if (glyphx > ImageWidth-Glyph.GLYPH_SIZE) continue;
                    if (glyphy > ImageHeight-Glyph.GLYPH_SIZE) continue;

                    var glyph = Glyph.FromImageData(this, glyphx, glyphy);
                    if (glyph.IsValid) {
                        glyph.MarkUsed(this);
                        glyphs.Add(glyph);
                    }
                }
            }
        }

        return [.. glyphs.OrderBy(x => x.Y).ThenBy(x => x.X)];
    }
}
