/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See src/Resources/Files/License.txt for full licensing and attribution      //
// details.                                                                    //
// .                                                                           //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Text;

namespace PixelDotNet
{
    internal interface IGradientConfig
    {
        event EventHandler GradientInfoChanged;

        GradientInfo GradientInfo
        {
            get;
            set;
        }

        void PerformGradientInfoChanged();
    }
}
