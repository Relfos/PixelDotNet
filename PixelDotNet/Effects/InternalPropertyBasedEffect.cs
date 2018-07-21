/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) Rick Brewster, Tom Jackson, and past contributors.            //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See src/Resources/Files/License.txt for full licensing and attribution      //
// details.                                                                    //
// .                                                                           //
/////////////////////////////////////////////////////////////////////////////////

using SpriteDotNet.Core;
using SpriteDotNet.IndirectUI;
using SpriteDotNet.PropertySystem;
using System;
using System.Drawing;

namespace SpriteDotNet.Effects
{
    public abstract class InternalPropertyBasedEffect
        : PropertyBasedEffect
    {
        protected override void OnCustomizeConfigUIWindowProperties(PropertyCollection props)
        {
            props[ControlInfoPropertyNames.WindowIsSizable].Value = false;
            base.OnCustomizeConfigUIWindowProperties(props);
        }

        internal InternalPropertyBasedEffect(string name, Image image, string subMenuName, EffectFlags flags)
            : base(name, image, subMenuName, flags)
        {
        }
    }
}
