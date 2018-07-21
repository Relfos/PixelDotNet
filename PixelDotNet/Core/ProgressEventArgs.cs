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
    public class ProgressEventArgs
        : System.EventArgs
    {
        private double percent;
        public double Percent
        {
            get
            {
                return percent;
            }
        }

        public ProgressEventArgs(double percent)
        {
            this.percent = percent;
        }
    }
}
