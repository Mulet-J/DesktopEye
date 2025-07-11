using OpenCvSharp;

namespace DesktopEye.Common.Tests.Services.Ocr; 

public class OpenCvSharpTest
{
    public static bool ConvertToGrayscale(string inputImagePath, string outputFolderPath, string outputFileName = null)
    {
        try
        {
            // Validate input parameters
            if (string.IsNullOrEmpty(inputImagePath) || !File.Exists(inputImagePath))
            {
                Console.WriteLine("Input image file does not exist or path is invalid.");
                return false;
            }

            if (string.IsNullOrEmpty(outputFolderPath))
            {
                Console.WriteLine("Output folder path cannot be null or empty.");
                return false;
            }

            // Create output directory if it doesn't exist
            if (!Directory.Exists(outputFolderPath))
            {
                Directory.CreateDirectory(outputFolderPath);
                Console.WriteLine($"Created output directory: {outputFolderPath}");
            }

            // Generate output filename if not provided
            if (string.IsNullOrEmpty(outputFileName))
            {
                string originalFileName = Path.GetFileNameWithoutExtension(inputImagePath);
                string extension = Path.GetExtension(inputImagePath);
                outputFileName = $"{originalFileName}_grayscale{extension}";
            }

            string outputPath = Path.Combine(outputFolderPath, outputFileName);

            // Load the image
            using (Mat originalImage = Cv2.ImRead(inputImagePath, ImreadModes.Color))
            {
                if (originalImage.Empty())
                {
                    Console.WriteLine("Failed to load the image. Please check if the file is a valid image format.");
                    return false;
                }

                Console.WriteLine($"Original image loaded: {originalImage.Width}x{originalImage.Height} pixels");

                // Convert to grayscale
                using (Mat grayscaleImage = new Mat())
                {
                    Cv2.CvtColor(originalImage, grayscaleImage, ColorConversionCodes.BGR2GRAY);
                    
                    // Save the grayscale image
                    bool saveSuccess = Cv2.ImWrite(outputPath, grayscaleImage);
                    
                    if (saveSuccess)
                    {
                        Console.WriteLine($"Grayscale image saved successfully to: {outputPath}");
                        return true;
                    }
                    else
                    {
                        Console.WriteLine("Failed to save the grayscale image.");
                        return false;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred during image processing: {ex.Message}");
            return false;
        }
    }

    [Fact]
    public void Azer()
    {
        // Example 1: Convert from file path
        string inputPath = "/Users/remi.marques/Documents/Capture d’écran 2025-06-10 à 10.16.24.png";
        string outputFolder = "/Users/remi.marques/Documents/output";
        
        bool success = ConvertToGrayscale(inputPath, outputFolder);
        
        if (success)
        {
            Console.WriteLine("Image conversion completed successfully!");
        }
        else
        {
            Console.WriteLine("Image conversion failed.");
        }

        // Example 2: Convert with custom output filename
        bool success2 = ConvertToGrayscale(
            inputPath, 
            outputFolder, 
            "my_grayscale_image.jpg"
        );
        
    }
}