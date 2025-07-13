using System;
using System.IO;
using OpenCvSharp;

namespace DesktopEye.Common.Domain.Features.OpticalCharacterRecognition;

public static class ImagePreprocessor
{
    public struct PreprocessingOptions
    {
        public Size? TargetSize { get; set; }
        public double ClaheLimitThreshold { get; set; }
        public Size ClaheGridSize { get; set; }
        public bool EnableDenoising { get; set; }
        public bool EnableMorphologicalCleanup { get; set; }
        public bool IsDebugImageSaved { get; set; }
        public string DebugImagePath { get; set; }
        public ThresholdTypes ThresholdMethod { get; set; }

        public static PreprocessingOptions Default => new()
        {
            TargetSize = new Size(2000, 2000),
            ClaheLimitThreshold = 3.0,
            ClaheGridSize = new Size(8, 8),
            EnableDenoising = true,
            EnableMorphologicalCleanup = true,
            IsDebugImageSaved = false,
            DebugImagePath = "/home/rzn/.local/share/DesktopEye/debug/processedImage.png",
            ThresholdMethod = ThresholdTypes.Binary | ThresholdTypes.Otsu
        };
    }

    /// <summary>
    /// Preprocesses an image to optimize OCR recognition
    /// </summary>
    /// <param name="inputMat">Input image (will be modified)</param>
    /// <param name="options">Preprocessing options</param>
    /// <returns>The preprocessed image (same reference as inputMat)</returns>
    public static Mat PreprocessImage(Mat inputMat, PreprocessingOptions? options = null)
    {
        if (inputMat.Empty())
            throw new ArgumentException(@"Input image cannot be null or empty", nameof(inputMat));

        var opts = options ?? PreprocessingOptions.Default;

        try
        {
            // Step 1: Convert to grayscale
            ConvertToGrayscale(inputMat);

            // Step 2: Normalize orientation (black text on white background)
            NormalizeTextOrientation(inputMat);

            // Step 3: Resize with aspect ratio preservation
            if (opts.TargetSize.HasValue)
            {
                ResizeWithAspectRatio(inputMat, opts.TargetSize.Value);
            }

            // Step 4: Denoising (optional)
            if (opts.EnableDenoising)
            {
                ApplyDenoising(inputMat);
            }

            // Step 5: Adaptive contrast enhancement
            EnhanceContrast(inputMat, opts.ClaheLimitThreshold, opts.ClaheGridSize);

            // Step 6: Light smoothing to reduce noise
            ApplyGaussianBlur(inputMat);

            // Step 7: Binarization
            ApplyThresholding(inputMat, opts.ThresholdMethod);

            // Step 8: Morphological cleanup (optional)
            if (opts.EnableMorphologicalCleanup)
            {
                ApplyMorphologicalCleanup(inputMat);
            }

            // Step 9: Debug image saving (optional)
            if (opts.IsDebugImageSaved && !string.IsNullOrEmpty(opts.DebugImagePath))
            {
                SaveDebugImage(inputMat, opts.DebugImagePath);
            }

            return inputMat;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error during image preprocessing: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Converts the image to grayscale if necessary
    /// </summary>
    private static void ConvertToGrayscale(Mat mat)
    {
        switch (mat.Channels())
        {
            case 1:
                return; // Already grayscale
            case 3:
                Cv2.CvtColor(mat, mat, ColorConversionCodes.BGR2GRAY);
                break;
            case 4:
                Cv2.CvtColor(mat, mat, ColorConversionCodes.BGRA2GRAY);
                break;
            default:
                throw new ArgumentException($"Unsupported number of channels: {mat.Channels()}");
        }
    }

    /// <summary>
    /// Ensures text is black on white background
    /// </summary>
    private static void NormalizeTextOrientation(Mat image)
    {
        if (image.Channels() != 1)
            throw new ArgumentException("Image must be grayscale");

        using var binary = new Mat();
        Cv2.Threshold(image, binary, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);

        if (ShouldInvertImage(binary))
        {
            Cv2.BitwiseNot(image, image);
        }
    }

    /// <summary>
    /// Determines if the image should be inverted based on border region analysis
    /// </summary>
    private static bool ShouldInvertImage(Mat binaryImage)
    {
        var height = binaryImage.Rows;
        var width = binaryImage.Cols;

        // Analyze border regions (more likely to be background)
        var borderRegions = new[]
        {
            new Rect(0, 0, width, height / 10), // Top border
            new Rect(0, 9 * height / 10, width, height / 10), // Bottom border
            new Rect(0, 0, width / 10, height), // Left border
            new Rect(9 * width / 10, 0, width / 10, height) // Right border
        };

        var totalPixels = 0;
        var whitePixels = 0;

        foreach (var region in borderRegions)
        {
            using var roi = new Mat(binaryImage, region);
            var mean = Cv2.Mean(roi);
            var regionPixels = region.Width * region.Height;

            totalPixels += regionPixels;
            whitePixels += (int)(mean.Val0 * regionPixels / 255.0);
        }

        // If more than 70% of border pixels are white, probably white background
        var whiteRatio = (double)whitePixels / totalPixels;
        return whiteRatio < 0.3; // Invert if dark background
    }

    /// <summary>
    /// Resizes the image while preserving aspect ratio
    /// </summary>
    private static void ResizeWithAspectRatio(Mat image, Size targetSize)
    {
        var currentAspectRatio = (double)image.Width / image.Height;
        var targetAspectRatio = (double)targetSize.Width / targetSize.Height;

        Size newSize;
        if (currentAspectRatio > targetAspectRatio)
        {
            // Image is wider than target
            newSize = new Size(targetSize.Width, (int)(targetSize.Width / currentAspectRatio));
        }
        else
        {
            // Image is taller than target
            newSize = new Size((int)(targetSize.Height * currentAspectRatio), targetSize.Height);
        }

        // Use INTER_AREA for downscaling, INTER_CUBIC for upscaling
        var interpolation = (newSize.Width * newSize.Height) < (image.Width * image.Height)
            ? InterpolationFlags.Area
            : InterpolationFlags.Cubic;

        Cv2.Resize(image, image, newSize, 0, 0, interpolation);
    }

    /// <summary>
    /// Applies adaptive denoising
    /// </summary>
    private static void ApplyDenoising(Mat image)
    {
        using var denoised = new Mat();

        // Use bilateral filter to preserve edges
        Cv2.BilateralFilter(image, denoised, 9, 80, 80);
        denoised.CopyTo(image);
    }

    /// <summary>
    /// Enhances contrast using adaptive CLAHE
    /// </summary>
    private static void EnhanceContrast(Mat image, double clipLimit, Size gridSize)
    {
        using var clahe = Cv2.CreateCLAHE(clipLimit, gridSize);
        using var enhanced = new Mat();

        clahe.Apply(image, enhanced);
        enhanced.CopyTo(image);
    }

    /// <summary>
    /// Applies light Gaussian blur
    /// </summary>
    private static void ApplyGaussianBlur(Mat image)
    {
        using var blurred = new Mat();
        Cv2.GaussianBlur(image, blurred, new Size(3, 3), 0);
        blurred.CopyTo(image);
    }

    /// <summary>
    /// Applies binarization with the specified method
    /// </summary>
    private static void ApplyThresholding(Mat image, ThresholdTypes method)
    {
        using var binary = new Mat();
        Cv2.Threshold(image, binary, 0, 255, method);
        binary.CopyTo(image);
    }

    /// <summary>
    /// Applies optimized morphological cleanup
    /// </summary>
    private static void ApplyMorphologicalCleanup(Mat binaryImage)
    {
        // Remove small noise with smaller structuring element
        using var kernel1 = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(2, 2));
        using var opened = new Mat();
        Cv2.MorphologyEx(binaryImage, opened, MorphTypes.Open, kernel1);

        // Close small gaps in characters
        using var kernel2 = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(3, 1));
        using var closed = new Mat();
        Cv2.MorphologyEx(opened, closed, MorphTypes.Close, kernel2);

        // Vertical closing for characters with vertical bars
        using var kernel3 = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(1, 3));
        using var finalResult = new Mat();
        Cv2.MorphologyEx(closed, finalResult, MorphTypes.Close, kernel3);

        finalResult.CopyTo(binaryImage);
    }

    /// <summary>
    /// Safely saves debug image
    /// </summary>
    private static void SaveDebugImage(Mat image, string path)
    {
        try
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            image.SaveImage(path);
        }
        catch (Exception ex)
        {
            ;
        }
    }
}