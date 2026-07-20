using System;
using System.Collections.Generic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SourceAFIS;

public class FP_Matching
{
    public bool MatchFingerprint(byte[] providedFingerprint, List<byte[]> storedFingerprints)
    {
        try
        {
            var providedTemplate = CreateFingerprintTemplate(providedFingerprint, 500);
            var matcher = new FingerprintMatcher(providedTemplate);
            double thresholdScore = 30.1;

            foreach (var storedFingerprint in storedFingerprints)
            {
                var storedTemplate = CreateFingerprintTemplate(storedFingerprint, 500);
                double score = matcher.Match(storedTemplate);

                if (score >= thresholdScore)
                    return true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during fingerprint matching: {ex.Message}");
            throw;
        }

        return false;
    }

    private (int width, int height) GetFingerprintDimensions(int dataLength)
    {
        var knownResolutions = new Dictionary<int, (int width, int height)>
        {
            { 300 * 400, (300, 400) },
            { 260 * 300, (260, 300) },
            { 256 * 288, (256, 288) }
        };

        if (knownResolutions.TryGetValue(dataLength, out var dimensions))
            return dimensions;

        throw new ArgumentException("Unknown fingerprint data length; dimensions cannot be determined.");
    }

    private FingerprintTemplate CreateFingerprintTemplate(byte[] fingerprintData, int dpi)
    {
        try
        {
            var (width, height) = GetFingerprintDimensions(fingerprintData.Length);
            using (var grayscaleImage = ToGrayscale(fingerprintData, width, height, dpi))
            {
                // Convert ImageSharp image to your SDK's expected format
                byte[] pixelData = new byte[width * height];
                grayscaleImage.ProcessPixelRows(accessor =>
                {
                    for (int y = 0; y < height; y++)
                    {
                        var row = accessor.GetRowSpan(y);
                        for (int x = 0; x < width; x++)
                        {
                            pixelData[y * width + x] = row[x].R;
                        }
                    }
                });

                var options = new FingerprintImageOptions { Dpi = dpi };
                var fingerprintImage = new FingerprintImage(width, height, pixelData, options);

                return new FingerprintTemplate(fingerprintImage);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating fingerprint template: {ex.Message}");
            throw;
        }
    }

    public Image<Rgb24> ToGrayscale(byte[] buffer, int width, int height, int dpi)
    {
        if (buffer.Length != width * height)
            throw new ArgumentException("The image buffer size does not match the specified dimensions.");

        var image = new Image<Rgb24>(width, height);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                byte gray = buffer[y * width + x];
                image[x, y] = new Rgb24(gray, gray, gray);
            }
        }

        image.Metadata.VerticalResolution = dpi;
        image.Metadata.HorizontalResolution = dpi;

        return image;
    }
}