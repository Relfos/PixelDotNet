/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See src/Resources/Files/License.txt for full licensing and attribution      //
// details.                                                                    //
// .                                                                           //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Drawing.Drawing2D;

namespace PixelDotNet
{
    internal interface ISelectionDrawModeConfig
    {
        event EventHandler SelectionDrawModeInfoChanged;

        SelectionDrawModeInfo SelectionDrawModeInfo
        {
            get;
            set;
        }

        void PerformSelectionDrawModeInfoChanged();
    }
}
