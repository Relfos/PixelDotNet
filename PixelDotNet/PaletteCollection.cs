/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See src/Resources/Files/License.txt for full licensing and attribution      //
// details.                                                                    //
// .                                                                           //
/////////////////////////////////////////////////////////////////////////////////

using PixelDotNet.SystemLayer;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;

namespace PixelDotNet
{
    internal sealed class PaletteCollection
    {
        /// <summary>
        /// The required number of colors for a palette.
        /// </summary>
        /// <remarks>
        /// If a palette is loaded with fewer colors than this, then it will be padded with entries
        /// that are equal to DefaultColor. If a palette is loaded with more colors than this, then
        /// the 97th through the last color will be discarded.
        /// </remarks>
        public const int PaletteColorCount = 96;
        private const char lineCommentChar = ';';
        private static readonly Encoding paletteFileEncoding = Encoding.UTF8;

        private Dictionary<string, ColorBgra[]> palettes; // maps from Name -> Palette

        public static bool ValidatePaletteName(string paletteName)
        {
            if (string.IsNullOrEmpty(paletteName))
            {
                return false;
            }

            try
            {
                string fileName = Path.ChangeExtension(paletteName, PalettesFileExtension);
                string pathName = Path.Combine(PalettesPath, fileName);
                char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
                char[] invalidPathNameChars = Path.GetInvalidPathChars();

                if (pathName.IndexOfAny(invalidPathNameChars) != -1)
                {
                    return false;
                }

                if (fileName.IndexOfAny(invalidFileNameChars) != -1)
                {
                    return false;
                }

                return true;
            }

            catch (ArgumentNullException)
            {
                return false;
            }

            catch (ArgumentException)
            {
                return false;
            }
        }

        public static ColorBgra DefaultColor
        {
            get
            {
                return ColorBgra.White;
            }
        }

        public static ColorBgra[] DefaultPalette
        {
            get
            {
                var paletteImage = PdnResources.GetImage("Images.DefaultPalette.png");
                var img = new Bitmap(paletteImage);
                var colors = new List<ColorBgra>();
                for (int j = 0; j < paletteImage.Height; j++)
                {
                    for (int i = 0; i < paletteImage.Width; i++)
                    {
                        var color = img.GetPixel(i, j);
                        colors.Add(new ColorBgra() { R = color.R, G = color.G, B = color.B, A = 255 });
                    }
                }
                return colors.ToArray();
            }
        }

        public string[] PaletteNames
        {
            get
            {
                Dictionary<string, ColorBgra[]>.KeyCollection keyCollection = this.palettes.Keys;
                string[] keys = new string[keyCollection.Count];
                int index = 0;

                foreach (string key in keyCollection)
                {
                    keys[index] = key;
                    ++index;
                }

                return keys;
            }
        }

        public PaletteCollection()
        {
            this.palettes = new Dictionary<string, ColorBgra[]>();
        }

        public static string PalettesFileExtension
        {
            get
            {
                // seems like using .txt is just simpler: makes it obvious that it is human readable/writable, and not xml (which has high perf costs @ startup)
                return ".txt";
            }
        }

        public static string PalettesPath
        {
            get
            {
                string userDataPath = PdnInfo.UserDataPath;
                string palettesDirName = PdnResources.GetString("ColorPalettes.UserDataSubDirName");
                string palettesPath = Path.Combine(userDataPath, palettesDirName);
                return palettesPath;
            }
        }

        private static bool ParseColor(string colorString, out ColorBgra color)
        {
            bool returnVal;

            try
            {
                color = ColorBgra.ParseHexString(colorString);
                returnVal = true;
            }

            catch (Exception ex)
            {
                Tracing.Ping("Exception while parsing color string '" + colorString + "' :" + ex.ToString());
                color = DefaultColor;
                returnVal = false;
            }

            return returnVal;
        }

        public static string RemoveComments(string line)
        {
            int commentIndex = line.IndexOf(lineCommentChar);

            if (commentIndex != -1)
            {
                return line.Substring(0, commentIndex);
            }
            else
            {
                return line;
            }
        }

        public static bool ParsePaletteLine(string line, out ColorBgra color)
        {
            color = DefaultColor;

            if (line == null)
            {
                return false;
            }

            string trimmed1 = RemoveComments(line);
            string trimmed = trimmed1.Trim();

            if (trimmed.Length == 0)
            {
                return false;
            }

            bool gotColor = ParseColor(trimmed, out color);
            return gotColor;
        }

        public static ColorBgra[] ParsePaletteString(string paletteString)
        {
            List<ColorBgra> palette = new List<ColorBgra>();
            StringReader sr = new StringReader(paletteString);

            while (true)
            {
                string line = sr.ReadLine();

                if (line == null)
                {
                    break;
                }

                ColorBgra color;
                bool gotColor = ParsePaletteLine(line, out color);

                if (gotColor && palette.Count < PaletteColorCount)
                {
                    palette.Add(color);
                }
            }

            return palette.ToArray();
        }

        public static ColorBgra[] LoadPalette(string palettePath)
        {
            ColorBgra[] palette = null;
            FileStream paletteFile = new FileStream(palettePath, FileMode.Open, FileAccess.Read, FileShare.Read);

            try
            {
                StreamReader sr = new StreamReader(paletteFile, paletteFileEncoding);

                try
                {
                    string paletteString = sr.ReadToEnd();
                    palette = ParsePaletteString(paletteString);
                }

                finally
                {
                    sr.Close(); // as per docs, this also closes paletteFile
                    sr = null;
                    paletteFile = null;
                }
            }

            finally
            {
                if (paletteFile != null)
                {
                    paletteFile.Close();
                    paletteFile = null;
                }
            }

            if (palette == null)
            {
                return new ColorBgra[0];
            }
            else
            {
                return palette;
            }
        }

        private static string FormatColor(ColorBgra color)
        {
            return color.ToHexString();
        }

        public static string GetPaletteSaveString(ColorBgra[] palette)
        {
            StringWriter sw = new StringWriter();

            string header = PdnResources.GetString("ColorPalette.SaveHeader");
            sw.WriteLine(header);

            foreach (ColorBgra color in palette)
            {
                string colorString = FormatColor(color);
                sw.WriteLine(colorString);
            }

            return sw.ToString();
        }

        public static void SavePalette(string palettePath, ColorBgra[] palette)
        {
            FileStream paletteFile = new FileStream(palettePath, FileMode.Create, FileAccess.Write, FileShare.Read);

            try
            {
                StreamWriter sw = new StreamWriter(paletteFile, paletteFileEncoding);

                try
                {
                    string paletteString = GetPaletteSaveString(palette);
                    sw.WriteLine(paletteString);
                }

                finally
                {
                    sw.Close(); // as per documentation, this closes paletteFile as well
                    sw = null;
                    paletteFile = null;
                }
            }

            finally
            {
                if (paletteFile != null)
                {
                    paletteFile.Close();
                    paletteFile = null;
                }
            }
        }

        private bool DoesPalettesPathExist()
        {
            string palettesPath = PalettesPath;
            bool returnVal;

            try
            {
                returnVal = Directory.Exists(palettesPath);
            }

            catch (Exception ex)
            {
                Tracing.Ping("Exception while querying whether palettes path, '" + palettesPath + "' exists: " + ex.ToString());
                returnVal = false;
            }

            return returnVal;
        }

        public static void EnsurePalettesPathExists()
        {
            string palettesPath = PalettesPath;

            try
            {
                if (!Directory.Exists(palettesPath))
                {
                    Directory.CreateDirectory(palettesPath);
                }
            }

            catch (Exception ex)
            {
                // Fail silently
                Tracing.Ping("Exception while ensuring that " + palettesPath + " exists: " + ex.ToString());
            }
        }

        public void Load()
        {
            if (!DoesPalettesPathExist())
            {
                // can't load anything! no custom palettes exist.
                // we really don't want to create this directory unless they've
                // saved anything in to it. this is especially important if they
                // install and then set their language. the path name is localized
                // so we only want the final name to be there.
                this.palettes = new Dictionary<string, ColorBgra[]>();
                return;
            }

            string[] pathNames = new string[0];

            try
            {
                pathNames = Directory.GetFiles(PalettesPath, "*" + PalettesFileExtension);
            }

            catch (Exception ex)
            {
                // Trace the error, but otherwise fail silently
                Tracing.Ping("Exception while retrieving list of palette filenames: " + ex.ToString());
            }

            // Now, load the palettes
            Dictionary<string, ColorBgra[]> newPalettes = new Dictionary<string, ColorBgra[]>();

            foreach (string pathName in pathNames)
            {
                ColorBgra[] palette = LoadPalette(pathName);
                ColorBgra[] goodPalette = EnsureValidPaletteSize(palette);
                string fileName = Path.GetFileName(pathName);
                string paletteName = Path.ChangeExtension(fileName, null);
                newPalettes.Add(paletteName, goodPalette);
            }

            this.palettes = newPalettes;
        }

        public void Save()
        {
            EnsurePalettesPathExists();

            string palettesPath = PalettesPath;

            foreach (string paletteName in this.palettes.Keys)
            {
                ColorBgra[] palette = this.palettes[paletteName];
                ColorBgra[] goodPalette = EnsureValidPaletteSize(palette);
                string fileName = Path.ChangeExtension(paletteName, PalettesFileExtension);
                string pathName = Path.Combine(palettesPath, fileName);
                SavePalette(pathName, goodPalette);
            }
        }

        public static ColorBgra[] EnsureValidPaletteSize(ColorBgra[] colors)
        {
            ColorBgra[] validPalette = new ColorBgra[PaletteColorCount];

            for (int i = 0; i < PaletteColorCount; ++i)
            {
                if (i >= colors.Length)
                {
                    validPalette[i] = DefaultColor;
                }
                else
                {
                    validPalette[i] = colors[i];
                }
            }

            return validPalette;
        }

        public ColorBgra[] Get(string name)
        {
            string existingKeyName;
            bool contains = Contains(name, out existingKeyName);

            if (contains)
            {
                ColorBgra[] colors = this.palettes[existingKeyName];
                return (ColorBgra[])colors.Clone();
            }
            else
            {
                return null;
            }
        }

        public bool Contains(string name, out string existingKeyName)
        {
            foreach (string key in this.palettes.Keys)
            {
                if (string.Compare(key, name, StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    existingKeyName = key;
                    return true;
                }
            }

            existingKeyName = null;
            return false;
        }

        public void AddOrUpdate(string name, ColorBgra[] colors)
        {
            if (colors.Length != PaletteColorCount)
            {
                throw new ArgumentException("palette must have exactly " + PaletteColorCount.ToString() + " colors (actual: " + colors.Length.ToString() + ")");
            }

            Delete(name);
            this.palettes.Add(name, colors);
        }

        public bool Delete(string name)
        {
            string existingKeyName;
            bool contains = Contains(name, out existingKeyName);

            if (contains)
            {
                this.palettes.Remove(existingKeyName);
                return true;
            }

            return false;
        }
    }
}
