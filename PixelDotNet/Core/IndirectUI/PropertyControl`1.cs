/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) Rick Brewster, Tom Jackson, and past contributors.            //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See src/Resources/Files/License.txt for full licensing and attribution      //
// details.                                                                    //
// .                                                                           //
/////////////////////////////////////////////////////////////////////////////////

using PixelDotNet.PropertySystem;
using System;
using System.Windows.Forms;

namespace PixelDotNet.IndirectUI
{
    internal abstract class PropertyControl<TValue, TProperty>
        : PropertyControl
          where TProperty : Property<TValue>
    {
        public new TProperty Property
        {
            get
            {
                return (TProperty)base.Property;
            }
        }

        internal PropertyControl(PropertyControlInfo propInfo)
            : base(propInfo)
        {
        }
    }
}
