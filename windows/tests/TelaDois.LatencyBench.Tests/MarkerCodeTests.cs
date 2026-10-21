using TelaDois.LatencyBench;

namespace TelaDois.LatencyBench.Tests;

public class MarkerCodeTests
{
    // Desenha o marcador como imagem cinza width x height (vizinho mais próximo), como a bancada faria.
    private static byte[] Render(bool[] blocks, int width, int height)
    {
        var gray = new byte[width * height];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var row = y * MarkerCode.Grid / height;
                var col = x * MarkerCode.Grid / width;
                gray[y * width + x] = blocks[row * MarkerCode.Grid + col] ? (byte)255 : (byte)0;
            }
        }
        return gray;
    }

    private static bool[] Pattern(string bits) => bits.Select(c => c == '1').ToArray();

    [Fact]
    public void SizeAndGridAreFixed()
    {
        Assert.Equal(64, MarkerCode.Size);
        Assert.Equal(4, MarkerCode.Grid);
    }

    [Theory]
    [InlineData(0x0000, "0000000000000000")]
    [InlineData(0x0001, "0000000000000001")]
    [InlineData(0x8000, "1000000000000000")]
    [InlineData(0xFFFF, "1111111111111111")]
    [InlineData(0xA5A5, "1010010110100101")]
    public void EncodeIsMostSignificantBitFirst(int code, string expected)
    {
        Assert.Equal(Pattern(expected), MarkerCode.Encode(code));
    }

    [Fact]
    public void EncodeKeepsOnlyTheLow16Bits()
    {
        Assert.Equal(MarkerCode.Encode(0x1234), MarkerCode.Encode(0x71234));
    }

    [Theory]
    [InlineData(64)]
    [InlineData(48)]
    public void RoundTripsRandomCodes(int side)
    {
        var rng = new Random(12345);
        for (var i = 0; i < 200; i++)
        {
            var code = rng.Next(0, 0x10000);
            var gray = Render(MarkerCode.Encode(code), side, side);
            Assert.Equal(code, MarkerCode.Decode(gray, side, side));
        }
    }

    [Fact]
    public void RoundTripsEdgeCodes()
    {
        foreach (var code in new[] { 0, 1, 0xFFFF, 0xA5A5, 0x5A5A })
        {
            var gray = Render(MarkerCode.Encode(code), 64, 64);
            Assert.Equal(code, MarkerCode.Decode(gray, 64, 64));
        }
    }

    [Fact]
    public void MidGrayIsTooBlurredToRead()
    {
        var gray = new byte[64 * 64];
        Array.Fill(gray, (byte)128);
        Assert.Null(MarkerCode.Decode(gray, 64, 64));
    }

    [Fact]
    public void OneBlurredBlockInvalidatesTheRead()
    {
        var gray = Render(MarkerCode.Encode(0xA5A5), 64, 64);
        // área 3×3 em volta do centro do bloco (linha 1, coluna 2): x = 40, y = 24
        for (var dy = -1; dy <= 1; dy++)
        {
            for (var dx = -1; dx <= 1; dx++)
            {
                gray[(24 + dy) * 64 + 40 + dx] = 128;
            }
        }
        Assert.Null(MarkerCode.Decode(gray, 64, 64));
    }

    [Fact]
    public void RoundTripsATwoThirdsRender()
    {
        // 2/3 do tamanho (1704×786 a partir de 2556×1179): o marcador de 64 px sai com 43 px.
        var rng = new Random(54321);
        for (var i = 0; i < 200; i++)
        {
            var code = rng.Next(0, 0x10000);
            var gray = Render(MarkerCode.Encode(code), 43, 43);
            Assert.Equal(code, MarkerCode.Decode(gray, 43, 43));
        }
    }

    [Fact]
    public void AveragesAroundTheCentreSoOneNoisyPixelDoesNotSpoilTheRead()
    {
        const int side = 43;
        var gray = Render(MarkerCode.Encode(0xA5A5), side, side);
        // ruído de compressão bem no centro de cada bloco (um pixel cinza): só a média 3×3 lê certo
        for (var row = 0; row < MarkerCode.Grid; row++)
        {
            for (var col = 0; col < MarkerCode.Grid; col++)
            {
                var y = (2 * row + 1) * side / (2 * MarkerCode.Grid);
                var x = (2 * col + 1) * side / (2 * MarkerCode.Grid);
                gray[y * side + x] = 128;
            }
        }
        Assert.Equal(0xA5A5, MarkerCode.Decode(gray, side, side));
    }

    [Theory]
    [InlineData(2556, 1179, 2556, 1178, 16, 16, 64, 64)] // monitor virtual, altura cortada para par
    [InlineData(1366, 768, 1366, 768, 16, 16, 64, 64)]   // monitor físico deste PC
    [InlineData(2556, 1179, 1704, 786, 11, 11, 43, 43)]  // 2/3
    [InlineData(2556, 1179, 1920, 886, 12, 12, 48, 48)]  // 1920×886
    public void CropFollowsTheEncodedSize(int mw, int mh, int ew, int eh, int x, int y, int w, int h)
    {
        Assert.Equal((x, y, w, h), MarkerCode.CropIn(mw, mh, ew, eh));
    }

    [Fact]
    public void MarkerSitsSixteenPixelsFromTheCorner()
    {
        Assert.Equal(16, MarkerCode.Offset);
    }

    [Fact]
    public void ToleratesModerateNoise()
    {
        var gray = Render(MarkerCode.Encode(0xA5A5), 64, 64);
        for (var i = 0; i < gray.Length; i++)
        {
            gray[i] = gray[i] == 255 ? (byte)230 : (byte)25;
        }
        Assert.Equal(0xA5A5, MarkerCode.Decode(gray, 64, 64));
    }

    [Fact]
    public void RejectsImagesThatAreTooSmallOrShort()
    {
        Assert.Null(MarkerCode.Decode(new byte[3], 64, 64));
        Assert.Null(MarkerCode.Decode(new byte[4], 2, 2));
    }
}
