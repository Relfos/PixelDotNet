/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See src/Resources/Files/License.txt for full licensing and attribution      //
// details.                                                                    //
// .                                                                           //
/////////////////////////////////////////////////////////////////////////////////

using SpriteDotNet;
using SpriteDotNet.PropertySystem;
using SpriteDotNet.Effects;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace SpriteDotNet.Effects
{
    /// <summary>
    /// ColorDifferenctEffect is a base class for my difference effects
    /// that have floating point (double) convolution filters.
    /// its architecture is just like ConvolutionFilterEffect, adding a
    /// function (RenderColorDifferenceEffect) called from Render in each
    /// derived class.
    /// It is also limited to 3x3 kernels.
    /// (Chris Crosetto)
    /// </summary>
    public abstract class ColorDifferenceEffect
        : InternalPropertyBasedEffect
    {            
        public unsafe void RenderColorDifferenceEffect(
            double[][] weights, 
            RenderArgs dstArgs, 
            RenderArgs srcArgs, 
            Rectangle[] rois, 
            int startIndex, 
            int length)
        {
            Surface dst = dstArgs.Surface;
            Surface src = srcArgs.Surface;

            for (int i = startIndex; i < startIndex + length; ++i)
            {
                Rectangle rect = rois[i];

                // loop through each line of target rectangle
                for (int y = rect.Top; y < rect.Bottom; ++y)
                {
                    int fyStart = 0;
                    int fyEnd = 3;

                    if (y == src.Bounds.Top) fyStart = 1;
                    if (y == src.Bounds.Bottom - 1) fyEnd = 2;

                    // loop through each point in the line 
                    ColorBgra* dstPtr = dst.GetPointAddressUnchecked(rect.Left, y);

                    for (int x = rect.Left; x < rect.Right; ++x)
                    {
                        int fxStart = 0;
                        int fxEnd = 3;

                        if (x == src.Bounds.Left)
                        {
                            fxStart = 1;
                        }

                        if (x == src.Bounds.Right - 1) 
                        {
                            fxEnd = 2;
                        }

                        // loop through each weight
                        double rSum = 0.0;
                        double gSum = 0.0;
                        double bSum = 0.0;

                        for (int fy = fyStart; fy < fyEnd; ++fy)
                        {
                            for (int fx = fxStart; fx < fxEnd; ++fx)
                            {
                                double weight = weights[fy][fx];
                                ColorBgra c = src.GetPointUnchecked(x - 1 + fx, y - 1 + fy);

                                rSum += weight * (double)c.R;
                                gSum += weight * (double)c.G;
                                bSum += weight * (double)c.B;
                            }
                        }

                        int iRsum = (int)rSum;
                        int iGsum = (int)gSum;
                        int iBsum = (int)bSum;

                        if (iRsum > 255)
                        {
                            iRsum = 255;
                        }

                        if (iGsum > 255)
                        {
                            iGsum = 255;
                        }

                        if (iBsum > 255)
                        {
                            iBsum = 255;
                        }

                        if (iRsum < 0)
                        {
                            iRsum = 0;
                        }

                        if (iGsum < 0)
                        {
                            iGsum = 0;
                        }

                        if (iBsum < 0)
                        {
                            iBsum = 0;
                        }

                        *dstPtr = ColorBgra.FromBgra((byte)iBsum, (byte)iGsum, (byte)iRsum, 255);
                        ++dstPtr;
                    }
                }
            }
        }

        internal ColorDifferenceEffect(string name, Image image, string subMenuName, EffectFlags flags)
            : base(name, image, subMenuName, flags)
        {
        }
    }    
}   