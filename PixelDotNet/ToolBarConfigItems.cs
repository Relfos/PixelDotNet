/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See src/Resources/Files/License.txt for full licensing and attribution      //
// details.                                                                    //
// .                                                                           //
/////////////////////////////////////////////////////////////////////////////////

using System;

namespace PixelDotNet
{
    [Flags]
    internal enum ToolBarConfigItems
        : uint
    {
        None = 0,
        All = ~None,

        // IMPORTANT: Keep these in alphabetical order.
        AlphaBlending = 1,
        Brush = 2,
        ColorPickerBehavior = 4,
        FloodMode = 2048,
        Gradient = 8,
        Pen = 16,
        PenCaps = 32,
        SelectionCombineMode = 1024,
        SelectionDrawMode = 4096,
        ShapeType = 64,
        Resampling = 128,
        Text = 256,     
        Tolerance = 512,
    }
}
