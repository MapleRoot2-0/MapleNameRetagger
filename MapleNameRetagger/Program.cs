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

using System.Drawing.Imaging;
using System.Xml.Linq;
using MapleNameRetagger;

try
{
    bool suppliedFileExists = args.Length > 1 && File.Exists(args[0]);
    string targetFile = suppliedFileExists ? args[0] : "NameTag.img.xml";
    string backupFile = $"{targetFile}.bak";

    // If the user didn't supply a path and fallback file is not found...
    if (!suppliedFileExists && !File.Exists(targetFile))
    {
        Console.WriteLine($"ERROR: No target file supplied and default file (.\\{targetFile}) not found. Process Terminates.");
        Environment.Exit(1);
    }

    if (File.Exists(backupFile))
    {
        File.Delete(backupFile);
    }

    File.Copy(targetFile, backupFile);

    using FileStream? stream = new FileStream(targetFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

    XDocument? document;

    try
    {
        document = XDocument.Load(stream);
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
        throw;
    }

    if (document == null || document.Root.IsEmpty)
    {
        Console.WriteLine("ERROR: File load failure or empty document.");

        return;
    }

    try
    {
        ProcessImageDir(document.Root.Attribute("name").Value, document.Root);

        Console.WriteLine("INFO: Finished processing, attempting to save!");

        // Ensure we are at the start of the stream
        stream.Seek(0, SeekOrigin.Begin);
        document.Save(stream);

        Console.WriteLine("INFO: Saved successfully!");
    }
    catch (Exception e)
    {
        Console.WriteLine($"ERROR: {e.Message}\r\n{e.StackTrace}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"ERROR: {ex.Message}\r\n{ex.StackTrace}");
}

return;

void ProcessImageDir(string baseName, XElement baseElement)
{
    if (baseElement == null)
    {
        return;
    }

    // Process canvas elements at the current level
    ProcessCanvasElements(baseName, baseElement);

    // Process each child element in the current imgdir
    foreach (var xElement in baseElement.Elements())
    {
        // If the child is an imgdir, recursively process it
        if (xElement.Name == "imgdir")
        {
            string newBaseName = $"{baseName}.{xElement.Attribute("name").Value}";
            Console.WriteLine($"New imgdir: {newBaseName}");
            ProcessImageDir(newBaseName, xElement);
        }
    }
}

void ProcessCanvasElements(string baseName, XElement parentElement)
{
    Dictionary<string, XElement> canvasItems = new ();
    int eHeight = 0, wOriginY = 0, eOriginY = 0;
    string? eBaseData = null;

    // Process only canvas elements in the current xml element
    foreach (var element in parentElement.Elements("canvas"))
    {
        string name = element.Attribute("name").Value;
        if (name != "e" && name != "w")
        {
            continue;
        }

        Console.WriteLine($"Processing: {baseName}.{name}");

        canvasItems[name] = element;

        int height = int.Parse(element.Attribute("height").Value);
        string baseData = element.Attribute("basedata")?.Value;
        XElement? vector = element.Element("vector");
        int originY = int.Parse(vector.Attribute("y").Value);

        switch (name)
        {
            case "e":
                eHeight = height;
                eOriginY = originY;
                eBaseData = baseData;
                break;
            case "w":
                wOriginY = originY;
                break;
        }
    }

    if (canvasItems.ContainsKey("e") && canvasItems.ContainsKey("w"))
    {
        int newPixels = wOriginY - eOriginY;
        int newHeight = eHeight + newPixels;

        if (newPixels >= 0 && newHeight != eHeight)
        {
            canvasItems["e"].Attribute("height").SetValue(newHeight);

            if (!string.IsNullOrWhiteSpace(eBaseData))
            {
                // Load image from xml, resize, save new image as base64 string  
                string newBaseData = ImageHelper.AddTransparentSpace(
                        ImageHelper.LoadImageFromBase64String(eBaseData), newHeight)
                    .SaveImageAsBase64String(ImageFormat.Png);

                canvasItems["e"].Attribute("basedata").SetValue(newBaseData);

                Console.WriteLine($"INFO: [{baseName}] => Applied edit to image and canvas.");
                return;
            }

            Console.WriteLine($"INFO: [{baseName}] => Applied edit to canvas.");
        }
    }
}
