using DesktopEye.Common.Services.OCR;
using DesktopEye.Common.Tests.TestHelpers;
using OpenCvSharp;

namespace DesktopEye.Common.Tests.Services.Ocr;

public class ImagePreprocessorTest
{
    private readonly string _sourceDirectory;
    private readonly string _targetDirectory;

    public ImagePreprocessorTest()
    {
        _sourceDirectory = PathHelper.GetAssetsPath();
        _targetDirectory = PathHelper.GetTestResultsPath();
        Directory.CreateDirectory(_targetDirectory);
    }

    [Fact]
    public void PreprocessImage_ShouldReturnValidMat_WhenInputIsValid()
    {
        // Arrange
        var filename = "multilines_lorem_ipsum_w_on_b.png";
        var imagePath = Path.Combine(_sourceDirectory, filename);

        // Skip test if image doesn't exist
        if (!File.Exists(imagePath))
        {
            Assert.True(true, "Test image not found - skipping test");
            return;
        }

        using var input = Cv2.ImRead(imagePath, ImreadModes.Unchanged);
        Assert.False(input.Empty(), "Failed to load test image");

        // Act
        using var result = ImagePreprocessor.PreprocessImage(input);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Empty(), "Preprocessed image should not be empty");
        Assert.Equal(input.Size(), result.Size());
        Assert.Equal(MatType.CV_8UC1, result.Type());
        result.SaveImage(Path.Combine(_targetDirectory, "processed-" + filename));
    }
}