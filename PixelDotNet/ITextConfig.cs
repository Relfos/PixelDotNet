﻿////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See src/Resources/Files/License.txt for full licensing and attribution      //
// details.                                                                    //
// .                                                                           //
/////////////////////////////////////////////////////////////////////////////////

using SpriteDotNet.SystemLayer;
using System;
using System.Drawing;

namespace SpriteDotNet
{
    internal interface ITextConfig
    {
        event EventHandler FontInfoChanged;
        event EventHandler FontSmoothingChanged;
        event EventHandler FontAlignmentChanged;

        FontInfo FontInfo
        {
            get;
            set;
        }

        FontFamily FontFamily 
        { 
            get; 
            set; 
        }

        float FontSize 
        { 
            get; 
            set; 
        }

        FontStyle FontStyle
        {
            get;
            set;
        }

        FontSmoothing FontSmoothing 
        { 
            get; 
            set; 
        }

        TextAlignment FontAlignment 
        { 
            get; 
            set; 
        }
    }
}
