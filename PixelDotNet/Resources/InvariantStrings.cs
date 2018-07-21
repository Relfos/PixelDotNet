/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See src/Resources/Files/License.txt for full licensing and attribution      //
// details.                                                                    //
// .                                                                           //
/////////////////////////////////////////////////////////////////////////////////

using System;

namespace SpriteDotNet
{
    /// <summary>
    /// Contains strings that must be the same no matter what locale the UI is running with.
    /// </summary>
    public static class InvariantStrings
    {
        // {0} is "All Rights Reserved"
        // Legal has advised that's the only part of this string that should be localizable.
        public const string CopyrightFormat = 
            "Copyright © 2008 dotPDN LLC, Rick Brewster, Tom Jackson, and contributors. Portions Copyright © Microsoft Corporation. {0}";

        public const string WebsiteUrl = "http://www.sprite.net";

        public const string WebsitePageHelpMenu = "/redirect/main_hm.html";

        public const string EffectsSubDir = "Effects";

        public const string FileTypesSubDir = "FileTypes";

        public const string DllExtension = ".dll";

        // Fallback strings are used in case the resources file is unavailable.
        public const string CrashLogHeaderTextFormatFallback =
            @"This text file was created because Paint.NET crashed.
Please e-mail this file to {0} so we can diagnose and fix the problem.
";

        public const string StartupUnhandledErrorFormatFallback =
            "There was an unhandled error, and Paint.NET must be closed. Refer to the file '{0}', which has been placed on your desktop, for more information.";

        public const string SingleInstanceMonikerName = "spritenet";
    }
}
