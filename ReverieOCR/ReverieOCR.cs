using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace ReverieOCR;

public class ReverieOCR : IGetPixel
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
    private int[] RawData {get;set;}

    public ReverieOCR(Bitmap image)
    {
        ImageWidth = image.Width;
        ImageHeight = image.Height;
        Bitmap = image;
        (RawData, ImageData) = GetImageData(image);
    }

    private static (int[], PixelInfo[]) GetImageData(Bitmap image)
    {
        var data = new int[image.Width*image.Height];

        BitmapData imageData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        Marshal.Copy(imageData.Scan0, data, 0, data.Length);
        image.UnlockBits(imageData);

        return (data, ConvertToImageData(data, image.Width, image.Height));
    } 

    private static PixelInfo[] ConvertToImageData(int[] pixeldata, int width, int height) {
        return pixeldata.Select(x => (x&0xFFFFFF) > 0x800000 ? PixelInfo.SET : PixelInfo.EMPTY).ToArray();
    }
    private static PixelInfo[] ConvertToImageDataIntensity(int[] pixeldata, int width, int height) {
        //(0.2126*R + 0.7152*G + 0.0722*B)
        return pixeldata.Select(argb => ((argb&0x00FF0000)*0.2126+(argb&0x0000FF00)*0.7152+(argb&0x000000FF)*0.0722) > 0.5 ? PixelInfo.SET : PixelInfo.EMPTY).ToArray();
    }

    public bool GetPixel(int x, int y) {
        if (y < 0 || y >= ImageHeight || x < 0 || x >= ImageWidth) {
            return false;
        }
        return ImageData[y*ImageWidth+x] == PixelInfo.SET;
    }

    public void MarkUsed(int x, int y)
    {
        if (y < 0 || y >= ImageHeight || x < 0 || x >= ImageWidth)
        {
            //Console.WriteLine("Position out of range {0}, {1} ({2}x{3})", x, y, ImageWidth, ImageHeight);
        }
        else
        {
            ImageData[y*ImageWidth+x] = ImageData[y*ImageWidth+x] == PixelInfo.SET ? PixelInfo.SET_PART_OF_GLYPH : PixelInfo.EMPTY_PART_OF_GLYPH;
        }
    }

    internal Glyph[] GetCharacters()
    {
        var glyphs = new List<Glyph>();

        for (var y = 0; y < ImageHeight; y++) {
            for (var x = 0; x < ImageWidth; x++) {
                if (GetPixel(x, y)) {
                    var pixelWrapper = this;

                    var left = pixelWrapper.GetPixel(x-1, y);
                    var top = pixelWrapper.GetPixel(x, y-1);
                    var right = pixelWrapper.GetPixel(x+1, y);
                    var bottom = pixelWrapper.GetPixel(x, y+1);

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
                        if (!pixelWrapper.GetPixel(x+xdirection*length, y+ydirection*length)) {
                            break;
                        }
                    }

                    if (length != 4) continue;

                    var glyphx = x-xdirection;
                    var glyphy = y-ydirection;

                    // if it can't fit, continue
                    if (glyphx > ImageWidth-Glyph.GLYPH_SIZE) continue;
                    if (glyphy > ImageHeight-Glyph.GLYPH_SIZE) continue;

                    var glyph = Glyph.FromImageData(pixelWrapper, glyphx, glyphy);
                    if (glyph.IsValid) {
                        glyph.MarkUsed(pixelWrapper);
                        glyphs.Add(glyph);
                    }
                }
            }
        }

        return [.. glyphs.OrderBy(x => x.Y).ThenBy(x => x.X)];
    }

    internal void SaveImageData(string filename, Glyph[]? glyphs = null,
        string fontname = "Carlito", float fontsize = 6,
        int? colEmpty = 0,
        int? colSet = 0xFFFFFF,
        int? colEmptyGlyph = 0,
        int? colSetGlyph = 0
    )
    {
        var convertedData = ImageData.Select((pixel, index) => pixel switch {
            PixelInfo.EMPTY => colEmpty,
            PixelInfo.SET => colSet,
            PixelInfo.EMPTY_PART_OF_GLYPH => colEmptyGlyph,
            PixelInfo.SET_PART_OF_GLYPH => colSetGlyph,
            _ => Color.Blue.ToArgb(),
        } ?? RawData[index]).ToArray();

        using var bmp = new Bitmap(ImageWidth, ImageHeight);
        var data = bmp.LockBits(new Rectangle(0, 0, ImageWidth, ImageHeight), ImageLockMode.WriteOnly, PixelFormat.Format32bppRgb);

        Marshal.Copy(convertedData, 0, data.Scan0, convertedData.Length);

        bmp.UnlockBits(data);

        if (glyphs != null) {
            using var gfx = Graphics.FromImage(bmp);

            var font = new Font(fontname, fontsize);
            foreach (var glyph in glyphs)
            {
                //gfx.DrawRectangle(Pens.Red, new Rectangle(glyph.X-1, glyph.Y-1, 7, 7));
                gfx.DrawString(glyph.Mapped, font, Brushes.White, new RectangleF(glyph.X, glyph.Y, 6, 6), new StringFormat(StringFormat.GenericTypographic) { LineAlignment = StringAlignment.Center });
            }
        }
        bmp.Save(filename);
    }
}
