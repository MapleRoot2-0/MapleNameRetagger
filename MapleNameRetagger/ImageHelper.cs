/*
 * This is free and unencumbered software released into the public domain.
 *
 * Anyone is free to copy, modify, publish, use, compile, sell, or
 * distribute this software, either in source code form or as a compiled
 * binary, for any purpose, commercial or non-commercial, and by any
 * means.
 *
 * For more information, please refer to <http://unlicense.org/>
 */

using System.Drawing;
using System.Drawing.Imaging;

namespace MapleNameRetagger;

internal static class ImageHelper
{
    public static Image LoadImageFromBase64String(string base64String)
    {
        byte[] imageBytes = Convert.FromBase64String(base64String);
        using MemoryStream ms = new MemoryStream(imageBytes, 0, imageBytes.Length);
        return Image.FromStream(ms, true);
    }

    public static string SaveImageAsBase64String(this Image targetImage, ImageFormat format)
    {
        using MemoryStream ms = new MemoryStream();

        targetImage.Save(ms, format);

        return Convert.ToBase64String(ms.ToArray());
    }

    public static Image AddTransparentSpace(Image originalImage, int newHeight)
    {
        if (originalImage == null)
            throw new ArgumentNullException(nameof(originalImage));

        if (newHeight <= originalImage.Height)
            throw new ArgumentException("New height must be greater than the original image height.");

        int additionalSpace = newHeight - originalImage.Height;
        int topMargin = additionalSpace;

        // Create a new bitmap with the desired dimensions
        Bitmap newImage = new Bitmap(originalImage.Width, newHeight);

        using Graphics graphics = Graphics.FromImage(newImage);
        
        // Set the background to be transparent
        graphics.Clear(Color.Transparent);

        // Draw the original image onto the new bitmap, from the bottom up
        graphics.DrawImage(originalImage, 0, topMargin, originalImage.Width, originalImage.Height);

        return newImage;
    }
}
