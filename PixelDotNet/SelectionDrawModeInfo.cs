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
using System.ComponentModel;
using System.Runtime.Serialization;

namespace PixelDotNet
{
    [Serializable]
    internal sealed class SelectionDrawModeInfo
        : ICloneable<SelectionDrawModeInfo>      
    {
        private SelectionDrawMode drawMode;
        private double width;
        private double height;

        public SelectionDrawMode DrawMode
        {
            get
            {
                return this.drawMode;
            }
        }

        public double Width
        {
            get
            {
                return this.width;
            }
        }

        public double Height
        {
            get
            {
                return this.height;
            }
        }

        public override bool Equals(object obj)
        {
            SelectionDrawModeInfo asSDMI = obj as SelectionDrawModeInfo;

            if (asSDMI == null)
            {
                return false;
            }

            return (asSDMI.drawMode == this.drawMode) && (asSDMI.width == this.width) && (asSDMI.height == this.height);
        }

        public override int GetHashCode()
        {
            return unchecked(this.drawMode.GetHashCode() ^ this.width.GetHashCode() ^ this.height.GetHashCode());
        }

        public SelectionDrawModeInfo(SelectionDrawMode drawMode, double width, double height)
        {
            this.drawMode = drawMode;
            this.width = width;
            this.height = height;
        }

        public static SelectionDrawModeInfo CreateDefault()
        {
            return new SelectionDrawModeInfo(SelectionDrawMode.Normal, 4.0, 3.0);
        }

        public SelectionDrawModeInfo CloneWithNewDrawMode(SelectionDrawMode newDrawMode)
        {
            return new SelectionDrawModeInfo(newDrawMode, this.width, this.height);
        }

        public SelectionDrawModeInfo CloneWithNewWidth(double newWidth)
        {
            return new SelectionDrawModeInfo(this.drawMode, newWidth, this.height);
        }

        public SelectionDrawModeInfo CloneWithNewHeight(double newHeight)
        {
            return new SelectionDrawModeInfo(this.drawMode, this.width, newHeight);
        }

        public SelectionDrawModeInfo CloneWithNewWidthAndHeight(double newWidth, double newHeight)
        {
            return new SelectionDrawModeInfo(this.drawMode, newWidth, newHeight);
        }

        public SelectionDrawModeInfo Clone()
        {
            return new SelectionDrawModeInfo(this.drawMode, this.width, this.height);
        }

        object ICloneable.Clone()
        {
            return Clone();
        }
    }
}
