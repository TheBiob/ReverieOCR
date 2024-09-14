namespace ReverieOCR;

public interface IGetPixel
{
    public bool GetPixel(int x, int y);
    public void MarkUsed(int x, int y);
}
