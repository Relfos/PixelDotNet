/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See src/Resources/Files/License.txt for full licensing and attribution      //
// details.                                                                    //
// .                                                                           //
/////////////////////////////////////////////////////////////////////////////////

using LunarParser.XML;
using PixelDotNet.SystemLayer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Windows.Forms;

namespace PixelDotNet
{
    public static class PdnResources
    {
        private static string resourcePath;
        private const string ourNamespace = "PaintDotNet";
        private static Assembly ourAssembly;
        private static CultureInfo pdnCulture;
        private static string resourcesDir;

        private static Dictionary<string, string> _strings = null;

        public static string ResourcesDir
        {
            get
            {
                if (resourcesDir == null)
                {
                    resourcesDir = Path.GetDirectoryName(typeof(PdnResources).Assembly.Location);
                }

                return resourcesDir;
            }

            set
            {
                resourcesDir = value;
                Initialize();
            }
        }

        public static CultureInfo Culture
        {
            get
            {
                return pdnCulture;
            }

            set
            {
                System.Threading.Thread.CurrentThread.CurrentUICulture = value;
                Initialize();
            }
        }

        private static void Initialize()
        {
            resourcePath = "Resources/";
            ourAssembly = Assembly.GetExecutingAssembly();
            pdnCulture = CultureInfo.CurrentUICulture;

            var xml = File.ReadAllText(resourcePath + "Strings/EN.xml");
            var root = XMLReader.ReadFromString(xml);
            root = root["root"];

            _strings = new Dictionary<string, string>();

            foreach (var entry in root.Children)
            {
                if (entry.Name == "data")
                {
                    var key = entry.GetString("name");
                    var value = entry.GetNode("value").Value;

                    _strings[key] = System.Net.WebUtility.HtmlDecode(value);
                }
            }
        }

        static PdnResources()
        {
            Initialize();
        }

        public static void SetNewCulture(string newLocaleName)
        {
            // TODO, HACK: post-3.0 we must refactor and have an actual user data manager that can handle all this renaming
            string oldUserDataPath = PdnInfo.UserDataPath;
            string oldPaletteDirName = PdnResources.GetString("ColorPalettes.UserDataSubDirName");
            // END HACK

            CultureInfo newCI = new CultureInfo(newLocaleName);
            Settings.CurrentUser.SetString("LanguageName", newLocaleName);
            Culture = newCI;

            // TODO, HACK: finish up renaming
            string newUserDataPath = PdnInfo.UserDataPath;
            string newPaletteDirName = PdnResources.GetString("ColorPalettes.UserDataSubDirName");

            // 1. rename user data dir from old localized name to new localized name
            if (oldUserDataPath != newUserDataPath)
            {
                try
                {
                    Directory.Move(oldUserDataPath, newUserDataPath);
                }

                catch (Exception)
                {
                }
            }

            // 2. rename palette dir from old localized name (in new localized user data path) to new localized name
            string oldPalettePath = Path.Combine(newUserDataPath, oldPaletteDirName);
            string newPalettePath = Path.Combine(newUserDataPath, newPaletteDirName);

            if (oldPalettePath != newPalettePath)
            {
                try
                {
                    Directory.Move(oldPalettePath, newPalettePath);
                }

                catch (Exception)
                {
                }
            }
            // END HACK
        }
        
        public static string[] GetLocaleNameChain()
        {
            List<string> names = new List<string>();
            CultureInfo ci = pdnCulture;

            while (ci.Name != string.Empty)
            {
                names.Add(ci.Name);
                ci = ci.Parent;
            }

            return names.ToArray();
        }

        private static ResourceManager CreateResourceManager()
        {
            const string stringsFileName = "PaintDotNet.Strings.3";
            ResourceManager rm = ResourceManager.CreateFileBasedResourceManager(stringsFileName, ResourcesDir, null);
            return rm;
        }

        public static string GetString(string stringName)
        {
            if (_strings.ContainsKey(stringName))
            {
                return _strings[stringName];
            }

            Debug.WriteLine(stringName + " not found");
            return "??";
        }

        private static string ReplaceFirst(this string text, string search, string replace)
        {
            int pos = text.IndexOf(search);
            if (pos < 0)
            {
                return text;
            }
            return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
        }

        public static Stream GetResourceStream(string fileName)
        {
            Stream stream = null;

            var baseName = Path.GetFileNameWithoutExtension(fileName).ReplaceFirst(".", "/");
            var ext = Path.GetExtension(fileName);
            string filePath = resourcePath + baseName + ext;

            if (File.Exists(filePath))
            {
                stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            }

            if (stream == null)
            {
                string fullName = ourNamespace + "." + fileName;
                stream = ourAssembly.GetManifestResourceStream(fullName);
            }

            return stream;
        }

        public static Image GetImageBmpOrPng(string fileNameNoExt)
        {
            // using Path.ChangeExtension is not what we want; quite often filenames are "Icons.BlahBlahBlah"
            string fileNameBmp = fileNameNoExt + ".bmp";
            Image image = GetImage(fileNameBmp);

            if (image == null)
            {
                string fileNamePng = fileNameNoExt + ".png";
                image = GetImage(fileNamePng);
            }

            return image;
        }

        public static Image GetImage(string fileName)
        {
            Stream stream = GetResourceStream(fileName);

            Image image = null;
            if (stream != null)
            {
                image = LoadImage(stream);
            }

            return image;
        }

        private sealed class PdnImageResource
            : ImageResource
        {
            private string name;
            private static Dictionary<string, ImageResource> images;

            protected override Image Load()
            {
                return PdnResources.GetImage(this.name);
            }

            public static ImageResource Get(string name)
            {
                ImageResource ir;

                if (!images.TryGetValue(name, out ir))
                {
                    ir = new PdnImageResource(name);
                    images.Add(name, ir);
                }

                return ir;
            }

            static PdnImageResource()
            {
                images = new Dictionary<string, ImageResource>();
            }

            private PdnImageResource(string name)
                : base()
            {
                this.name = name;
            }

            private PdnImageResource(Image image)
                : base(image)
            {
                this.name = null;
            }                
        }

        public static ImageResource GetImageResource(string fileName)
        {
            return PdnImageResource.Get(fileName);
        }

        public static Icon GetIcon(string fileName)
        {
            Stream stream = GetResourceStream(fileName);
            Icon icon = null;

            if (stream != null)
            {
                icon = new Icon(stream);
            }

            return icon;
        }

        public static Icon GetIconFromImage(string fileName)
        {
            Stream stream = GetResourceStream(fileName);

            Icon icon = null;

            if (stream != null)
            {
                Image image = LoadImage(stream);
                icon = Icon.FromHandle(((Bitmap)image).GetHicon());
                image.Dispose();
                stream.Close();
            }

            return icon;
        }

        private static bool CheckForSignature(Stream input, byte[] signature)
        {
            long oldPos = input.Position;
            byte[] inputSig = new byte[signature.Length];
            int amountRead = input.Read(inputSig, 0, inputSig.Length);

            bool foundSig = false;
            if (amountRead == signature.Length)
            {
                foundSig = true;

                for (int i = 0; i < signature.Length; ++i)
                {
                    foundSig &= (signature[i] == inputSig[i]);
                }
            }

            input.Position = oldPos;
            return foundSig;
        }

        internal static string[] GetInstalledLocales()
        {
            return new string[] { "English" };
        }

        public static bool IsGdiPlusImageAllowed(Stream input)
        {
            byte[] wmfSig = new byte[] { 0xd7, 0xcd, 0xc6, 0x9a };
            byte[] emfSig = new byte[] { 0x01, 0x00, 0x00, 0x00 };

            // Check for and explicitely block WMF and EMF images
            return !(CheckForSignature(input, emfSig) || CheckForSignature(input, wmfSig));
        }

        public static Image LoadImage(string fileName)
        {
            using (FileStream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return LoadImage(stream);
            }
        }

        /// <summary>
        /// Loads an image from the given stream. The stream must be seekable.
        /// </summary>
        /// <param name="input">The Stream to load the image from.</param>
        public static Image LoadImage(Stream input)
        {
            /*
            if (!IsGdiPlusImageAllowed(input))
            {
                throw new IOException("File format is not supported");
            }
            */

            Image image = Image.FromStream(input);

            if (image.RawFormat == ImageFormat.Wmf || image.RawFormat == ImageFormat.Emf)
            {
                image.Dispose();
                throw new IOException("File format isn't supported");
            }

            return image;
        }
    }
}
