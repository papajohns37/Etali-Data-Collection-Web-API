using Etali_Data_Collection_Web_API.Configuration;
using Xunit;

namespace Etali.Tests.Configuration;

public class GameConfigurationTests
{
    private readonly GameConfiguration _configuration;

    public GameConfigurationTests()
    {
        _configuration = new GameConfiguration();
    }

    [Fact]
    public void AvailableStickers_HasCorrectCount()
    {
        // Assert
        Assert.Equal(10, _configuration.AvailableStickers.Length);
    }

    [Fact]
    public void AvailableStickers_HasExpectedValues()
    {
        // Assert
        var stickers = _configuration.AvailableStickers;
        Assert.Contains(6, stickers);
        Assert.Contains(5, stickers);
        Assert.Contains(4, stickers);
        Assert.Contains(3, stickers);
    }

    [Fact]
    public void AvailableStickers_HasCorrectDistribution()
    {
        // Assert
        var stickers = _configuration.AvailableStickers;
        Assert.Equal(1, stickers.Count(s => s == 6));  // One 6
        Assert.Equal(2, stickers.Count(s => s == 5));  // Two 5s
        Assert.Equal(6, stickers.Count(s => s == 4));  // Six 4s
        Assert.Equal(1, stickers.Count(s => s == 3));  // One 3
    }

    [Fact]
    public void AvailableStickers_AllValuesPositive()
    {
        // Assert
        Assert.All(_configuration.AvailableStickers, sticker =>
        {
            Assert.True(sticker > 0);
        });
    }
}
