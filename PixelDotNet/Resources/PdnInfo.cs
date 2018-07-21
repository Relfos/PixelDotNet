/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See src/Resources/Files/License.txt for full licensing and attribution      //
// details.                                                                    //
// .                                                                           //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Drawing;
using System.IO;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace PixelDotNet
{
    /// <summary>
    /// A few utility functions specific to PaintDotNet.exe
    /// </summary>
    public static class PdnInfo
    {
        private static Icon appIcon;
        public static Icon AppIcon
        {
            get
            {
                if (appIcon == null)
                {
                    Stream stream = PdnResources.GetResourceStream("Icons.PaintDotNet.ico");
                    appIcon = new Icon(stream);
                    stream.Close();
                }

                return appIcon;
            }
        }

        /// <summary>
        /// Gets the full path to where user customization files should be stored.
        /// </summary>
        /// <returns>
        /// User data files should include settings or customizations that don't go into data files such as *.PDN.
        /// An example of a user data file is a color palette.
        /// </returns>
        public static string UserDataPath
        {
            get
            {
                string myDocsPath = SystemLayer.Shell.GetVirtualPath(PixelDotNet.SystemLayer.VirtualFolderName.UserDocuments, true);
                string userDataDirName = PdnResources.GetString("SystemLayer.UserDataDirName");
                string userDataPath = Path.Combine(myDocsPath, userDataDirName);
                return userDataPath;
            }
        }

        private static StartupTestType startupTest = StartupTestType.None;
        public static StartupTestType StartupTest
        {
            get 
            {
                return startupTest; 
            }

            set 
            {
                startupTest = value; 
            }
        }

        private static bool isTestMode = false;
        public static bool IsTestMode
        {
            get
            {
                return isTestMode;
            }

            set
            {
                isTestMode = value;
            }
        }

        public static DateTime BuildTime
        {
            get
            {
                Version version = GetVersion();

                DateTime time = new DateTime(2000, 1, 1, 0, 0, 0);
                time = time.AddDays(version.Build);
                time = time.AddSeconds(version.Revision * 2);

                return time;
            }
        }
        
        public static bool IsDebugBuild
        {
            get
            {
#if DEBUG
                return true;
#else
                return false;
#endif
            }
        }

        public static string GetApplicationDir()
        {
            string appPath = Application.StartupPath;
            return appPath;
        }

        /// <summary>
        /// For final builds, returns a string such as "Paint.NET v2.6"
        /// For non-final builds, returns a string such as "Paint.NET v2.6 Beta 2"
        /// </summary>
        /// <returns></returns>

        public static string GetProductName()
        {
            string bareProductName = GetBareProductName();
            string productNameFormat = PdnResources.GetString("Application.ProductName.Format");
            string tag;

            tag = string.Empty;

            string version = GetVersionNumberString(GetVersion(), 2);

            string productName = string.Format(
                productNameFormat,
                bareProductName,
                version,
                tag);

            return productName;
        }

        /// <summary>
        /// Returns the bare product name, e.g. "Paint.NET"
        /// </summary>
        public static string GetBareProductName()
        {
            return PdnResources.GetString("Application.ProductName.Bare");
        }

        private static string copyrightString = null;
        public static string GetCopyrightString()
        {
            if (copyrightString == null)
            {
                string format = InvariantStrings.CopyrightFormat;
                string allRightsReserved = PdnResources.GetString("Application.Copyright.AllRightsReserved");
                copyrightString = string.Format(CultureInfo.CurrentCulture, format, allRightsReserved);
            }

            return copyrightString;
        }

        public static Version GetVersion()
        {
            return new Version(Application.ProductVersion);
        }

        /// <summary>
        /// Returns a full version string of the form: ApplicationConfiguration + BuildType + BuildVersion
        /// i.e.: "Beta 2 Debug build 1.0.*.*"
        /// </summary>
        /// <returns></returns>
        public static string GetVersionString()
        {
            string buildType =
#if DEBUG
                "Debug";
#else
                "Release";
#endif
                
            string versionFormat = PdnResources.GetString("PdnInfo.VersionString.Format");

            string versionText = string.Format(
                versionFormat, 
                "Beta", 
                buildType, 
                GetVersionNumberString(GetVersion(), 4));

            return versionText;
        }

        /// <summary>
        /// Returns a string for just the version number, i.e. "3.01"
        /// </summary>
        /// <returns></returns>
        public static string GetVersionNumberString(Version version, int fieldCount)
        {
            if (fieldCount < 1 || fieldCount > 4)
            {
                throw new ArgumentOutOfRangeException("fieldCount", "must be in the range [1, 4]");
            }

            StringBuilder sb = new StringBuilder();

            sb.Append(version.Major.ToString());

            if (fieldCount >= 2)
            {
                sb.AppendFormat(".{0}", version.Minor.ToString("D2"));
            }

            if (fieldCount >= 3)
            {
                sb.AppendFormat(".{0}", version.Build.ToString());
            }

            if (fieldCount == 4)
            {
                sb.AppendFormat(".{0}", version.Revision.ToString());
            }

            return sb.ToString();
        }

        /// <summary>
        /// Returns a version string that is presentable without the Paint.NET name. example: "version 2.5 Beta 5"
        /// </summary>
        /// <returns></returns>
        public static string GetFriendlyVersionString()
        {
            Version version = PdnInfo.GetVersion();
            string versionFormat = PdnResources.GetString("PdnInfo.FriendlyVersionString.Format");
            string configFormat = PdnResources.GetString("PdnInfo.FriendlyVersionString.ConfigWithSpace.Format");
            string config = string.Format(configFormat, "???");
            string configText = config;

            string versionText = string.Format(versionFormat, GetVersionNumberString(version, 2), configText);
            return versionText;
        }

        /// <summary>
        /// Returns the application name, with the version string. i.e., "Paint.NET v2.5 (Beta 2 Debug build 1.0.*.*)"
        /// </summary>
        /// <returns></returns>
        public static string GetFullAppName()
        {
            string fullAppNameFormat = PdnResources.GetString("PdnInfo.FullAppName.Format");
            string fullAppName = string.Format(fullAppNameFormat, PdnInfo.GetProductName(), GetVersionString());
            return fullAppName;
        }

        /// <summary>
        /// For final builds, this returns PdnInfo.GetProductName() (i.e., "Paint.NET v2.2")
        /// For non-final builds, this returns GetFullAppName()
        /// </summary>
        /// <returns></returns>
        public static string GetAppName()
        {
            if (!PdnInfo.IsDebugBuild)
            {
                return PdnInfo.GetProductName();
            }
            else
            {
                return GetFullAppName();
            }
        }

        public static void LaunchWebSite(IWin32Window owner)
        {
            LaunchWebSite(owner, null);
        }

        public static void LaunchWebSite(IWin32Window owner, string page)
        {
            string webSite = InvariantStrings.WebsiteUrl;

            Uri baseUri = new Uri(webSite);
            Uri uri;

            if (page == null)
            {
                uri = baseUri;
            }
            else
            {
                uri = new Uri(baseUri, page);
            }

            string url = uri.ToString();

            if (url.IndexOf("@") == -1)
            {
                OpenUrl(owner, url);
            }
        }

        public static bool OpenUrl(IWin32Window owner, string url)
        {
            bool result = SystemLayer.Shell.LaunchUrl(owner, url);

            if (!result)
            {
                string messageFormat = PdnResources.GetString("LaunchLink.Error.Format");
                string message = string.Format(messageFormat, url);
                MessageBox.Show(owner, message, PdnInfo.GetBareProductName(), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return result;
        }

        public static string GetNgenPath()
        {
            return GetNgenPath(false);
        }

        public static string GetNgenPath(bool force32bit)
        {
            string fxDir;

            if (UIntPtr.Size == 8 && !force32bit)
            {
                fxDir = "Framework64";
            }
            else
            {
                fxDir = "Framework";
            }

            string fxPathBase = @"%WINDIR%\Microsoft.NET\" + fxDir + @"\v";
            string fxPath = fxPathBase + Environment.Version.ToString(3) + @"\";
            string fxPathExp = System.Environment.ExpandEnvironmentVariables(fxPath);
            string ngenExe = Path.Combine(fxPathExp, "ngen.exe");

            return ngenExe;
        }
    }
}
