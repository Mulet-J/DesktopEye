using System;
using OpenCvSharp;
using Tensorflow;

namespace DesktopEye.Common.Services.OCR;

public static class ImagePreprocessor
{
    // TODO there's probably a lot of room for improvement
    public static Mat PreprocessImage(Mat mat)
    {
        // We need a grayscale mat
        Grayscale(mat);

        using (var inverted = EnsureBlackTextOnWhite(mat))
        {
            inverted.CopyTo(mat);
        }

        ResizeWithAspectRatio(mat, new Size(2000, 2000));

        using (var denoised = new Mat())
        {
            Cv2.BilateralFilter(mat, denoised, 5, 25, 25);
            denoised.CopyTo(mat);
        }

        using (var clahe = Cv2.CreateCLAHE(5.0, new Size(16, 16)))
        {
            using var enhanced = new Mat();
            clahe.Apply(mat, enhanced);
            enhanced.CopyTo(mat);
        }

        using (var blur = new Mat())
        {
            Cv2.GaussianBlur(mat, blur, new Size(5, 5), 0);
            blur.CopyTo(mat);
        }

        using (var binary = new Mat())
        {
            // Cv2.AdaptiveThreshold(mat, binary, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.Binary, 7, 2);
            Cv2.Threshold(mat, binary, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
            // Cv2.Threshold(binary, binary, 0, 255, ThresholdTypes.Otsu);
            binary.CopyTo(mat);
        }

        //
        using (var cleaned = MorphologicalCleanup(mat))
        {
            cleaned.CopyTo(mat);
        }

        mat.SaveImage("/home/rzn/.local/share/DesktopEye/debug/processedImage.png");

        return mat;
    }

    private static Mat EnsureBlackTextOnWhite(Mat image)
    {
        if (image.Channels() != 1)
            throw new ArgumentException("Image must be grayscale");

        // Create a binary version for analysis
        var binary = new Mat();
        Cv2.Threshold(image, binary, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);

        try
        {
            // Sample pixels from different regions to determine background color
            var height = binary.Rows;
            var width = binary.Cols;
            var sampleCount = 0;
            var whitePixelCount = 0;

            // Sample from corners and edges (likely background regions)
            int[] sampleRegions =
            {
                // Top edge
                0, width / 4, 0, height / 10,
                3 * width / 4, width, 0, height / 10,
                // Bottom edge  
                0, width / 4, 9 * height / 10, height,
                3 * width / 4, width, 9 * height / 10, height,
                // Left edge
                0, width / 10, height / 4, 3 * height / 4,
                // Right edge
                9 * width / 10, width, height / 4, 3 * height / 4
            };

            for (var i = 0; i < sampleRegions.Length; i += 4)
            {
                var x1 = sampleRegions[i];
                var x2 = sampleRegions[i + 1];
                var y1 = sampleRegions[i + 2];
                var y2 = sampleRegions[i + 3];

                for (var y = y1; y < y2; y += 2)
                for (var x = x1; x < x2; x += 2)
                    if (x < width && y < height)
                    {
                        var pixelValue = binary.At<byte>(y, x);
                        if (pixelValue > 128) whitePixelCount++;
                        sampleCount++;
                    }
            }

            // Alternative method: analyze overall image statistics
            var meanScalar = Cv2.Mean(binary);
            var meanValue = meanScalar.Val0;

            // Combine both methods for robust detection
            var edgeWhiteRatio = sampleCount > 0 ? (double)whitePixelCount / sampleCount : 0;
            var isDarkBackground = edgeWhiteRatio < 0.3 || meanValue < 100;

            if (isDarkBackground)
            {
                // Invert the original image
                var inverted = new Mat();
                Cv2.BitwiseNot(image, inverted);
                return inverted;
            }
            else
            {
                return image.Clone();
            }
        }
        finally
        {
            binary.Dispose();
        }
    }

    private static void ResizeWithAspectRatio(Mat image, Size targetSize)
    {
        var aspectRatio = (double)image.Width / image.Height;
        int newWidth, newHeight;

        if (aspectRatio > 1) // Landscape
        {
            newWidth = targetSize.Width;
            newHeight = (int)(targetSize.Width / aspectRatio);
        }
        else // Portrait or square
        {
            newHeight = targetSize.Height;
            newWidth = (int)(targetSize.Height * aspectRatio);
        }

        Cv2.Resize(image, image, new Size(newWidth, newHeight), 0, 0, InterpolationFlags.Lanczos4);
    }

    private static Mat MorphologicalCleanup(Mat binaryImage)
    {
        // Remove small noise
        var kernel1 = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(2, 2));
        var opened = new Mat();
        Cv2.MorphologyEx(binaryImage, opened, MorphTypes.Open, kernel1);
        kernel1.Dispose();

        // Close small gaps in characters
        var kernel2 = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(2, 1));
        var closed = new Mat();
        Cv2.MorphologyEx(opened, closed, MorphTypes.Close, kernel2);
        opened.Dispose();
        kernel2.Dispose();

        return closed;
    }

    private static void Grayscale(Mat mat)
    {
        switch (mat.Channels())
        {
            case 1:
                return;
            case 3:
                Cv2.CvtColor(mat, mat, ColorConversionCodes.BGR2GRAY);
                return;
            case 4:
                Cv2.CvtColor(mat, mat, ColorConversionCodes.BGRA2GRAY);
                return;
            default:
                throw new InvalidArgumentError("Unexpected amount of channels detected for the provided mat");
        }
    }
}