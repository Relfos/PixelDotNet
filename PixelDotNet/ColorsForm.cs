/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See src/Resources/Files/License.txt for full licensing and attribution      //
// details.                                                                    //
// .                                                                           //
/////////////////////////////////////////////////////////////////////////////////

using PixelDotNet.SystemLayer;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;

namespace PixelDotNet
{
    // TODO: rewrite this ... the code is out of control here as it has grown organically,
    //       and it's impossible to maintain. post-3.0
    internal class ColorsForm 
        : FloatingToolForm
    {
        // We want some buttons that don't have a gradient background or fancy border
        private sealed class OurToolStripRenderer
            : ToolStripProfessionalRenderer
        {
            protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
            {
                if (e.ToolStrip is ToolStripDropDown)
                {
                    base.OnRenderToolStripBackground(e);
                }
                else
                {
                    using (SolidBrush backBrush = new SolidBrush(e.BackColor))
                    {
                        e.Graphics.FillRectangle(backBrush, e.AffectedBounds);
                    }
                }
            }

            protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
            {
                // Do not render a border.
            }
        }

        private System.ComponentModel.Container components = null;
 
        private int ignoreChangedEvents = 0;
        private ColorBgra lastPrimaryColor;
        private ColorBgra lastSecondaryColor;

        private int suspendSetWhichUserColor;
        private uint ignore = 0;
        private SwatchControl swatchControl;
        private ColorDisplayWidget colorDisplayWidget;

        private PaletteCollection paletteCollection = null;

        public PaletteCollection PaletteCollection
        {
            get
            {
                return this.paletteCollection;
            }

            set
            {
                this.paletteCollection = value;
            }
        }

        private bool IgnoreChangedEvents
        {
            get
            {
                return this.ignoreChangedEvents != 0;
            }
        }

        private class WhichUserColorWrapper
        {
            private WhichUserColor whichUserColor;

            public WhichUserColor WhichUserColor
            {
                get
                {
                    return this.whichUserColor;
                }
            }

            public override int GetHashCode()
            {
                return this.whichUserColor.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                WhichUserColorWrapper rhs = obj as WhichUserColorWrapper;

                if (rhs == null)
                {
                    return false;
                }

                if (rhs.whichUserColor == this.whichUserColor)
                {
                    return true;
                }

                return false;
            }

            public override string ToString()
            {
                return PdnResources.GetString("WhichUserColor." + this.whichUserColor.ToString());
            }

            public WhichUserColorWrapper(WhichUserColor whichUserColor)
            {
                this.whichUserColor = whichUserColor;
            }
        }

        public void SuspendSetWhichUserColor()
        {
            ++this.suspendSetWhichUserColor;
        }

        public void ResumeSetWhichUserColor()
        {
            --this.suspendSetWhichUserColor;
        }

        public event ColorEventHandler UserPrimaryColorChanged;
        protected virtual void OnUserPrimaryColorChanged(ColorBgra newColor)
        {
            if (UserPrimaryColorChanged != null && ignore == 0)
            {
                this.userPrimaryColor = newColor;
                UserPrimaryColorChanged(this, new ColorEventArgs(newColor));
                this.lastPrimaryColor = newColor;
                this.colorDisplayWidget.UserPrimaryColor = newColor;
            }
        }

        private ColorBgra userPrimaryColor;
        public ColorBgra UserPrimaryColor
        {
            get
            {
                return userPrimaryColor;
            }

            set
            {
                if (IgnoreChangedEvents)
                {
                    return;
                }

                if (userPrimaryColor != value)
                {
                    userPrimaryColor = value;
                    OnUserPrimaryColorChanged(value);

                    Update();
                    
                    this.colorDisplayWidget.UserPrimaryColor = this.userPrimaryColor;
                }
            }
        }

        private string GetHexNumericUpDownValue(int red, int green, int blue)
        {
            int newHexNumber = (red << 16) | (green << 8) | blue;
            string newHexText = System.Convert.ToString(newHexNumber, 16);
            
            while (newHexText.Length < 6)
            {
                newHexText = "0" + newHexText;
            }

            return newHexText.ToUpper();
        }

        public event ColorEventHandler UserSecondaryColorChanged;
        protected virtual void OnUserSecondaryColorChanged(ColorBgra newColor)
        {
            if (UserSecondaryColorChanged != null && ignore == 0)
            {
                this.userSecondaryColor = newColor;
                UserSecondaryColorChanged(this, new ColorEventArgs(newColor));
                this.lastSecondaryColor = newColor;
                this.colorDisplayWidget.UserSecondaryColor = newColor;
            }
        }

        private ColorBgra userSecondaryColor;
        public ColorBgra UserSecondaryColor
        {
            get
            {
                return userSecondaryColor;
            }

            set
            {
                if (IgnoreChangedEvents)
                {
                    return;
                }

                if (userSecondaryColor != value)
                {
                    userSecondaryColor = value;
                    OnUserSecondaryColorChanged(value);

                    Update();
                    this.colorDisplayWidget.UserSecondaryColor = this.userSecondaryColor;
                }
            }
        }

        public ColorsForm()
        {
            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();

            this.Text = PdnResources.GetString("ColorsForm.Text");

            // Load the current palette
            string currentPaletteString;

            try
            {
                currentPaletteString = Settings.CurrentUser.GetString(SettingNames.CurrentPalette, null);
            }

            catch (Exception)
            {
                currentPaletteString = null;
            }

            if (currentPaletteString == null)
            {
                string defaultPaletteString = PaletteCollection.GetPaletteSaveString(PaletteCollection.DefaultPalette);
                currentPaletteString = defaultPaletteString;
            }

            ColorBgra[] currentPalette = PaletteCollection.ParsePaletteString(currentPaletteString);

            this.swatchControl.Colors = currentPalette;

            this.swatchControl.Colors = PaletteCollection.DefaultPalette;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                    components = null;
                }
            }

            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.swatchControl = new PixelDotNet.SwatchControl();
            this.colorDisplayWidget = new PixelDotNet.ColorDisplayWidget();
            this.SuspendLayout();
            // 
            // swatchControl
            // 
            this.swatchControl.BlinkHighlight = false;
            this.swatchControl.Colors = new PixelDotNet.ColorBgra[0];
            this.swatchControl.Location = new System.Drawing.Point(8, 60);
            this.swatchControl.Name = "swatchControl";
            this.swatchControl.Size = new System.Drawing.Size(7*32, 10*32);
            this.swatchControl.TabIndex = 31;
            this.swatchControl.Text = "swatchControl1";
            this.swatchControl.ColorsChanged += this.SwatchControl_ColorsChanged;
            this.swatchControl.ColorClicked += this.SwatchControl_ColorClicked;
            // 
            // colorDisplayWidget
            // 
            this.colorDisplayWidget.Location = new System.Drawing.Point(4, 4);
            this.colorDisplayWidget.Name = "colorDisplayWidget";
            this.colorDisplayWidget.Size = new System.Drawing.Size(52, 52);
            this.colorDisplayWidget.TabIndex = 32;
            this.colorDisplayWidget.BlackAndWhiteButtonClicked += ColorDisplayWidget_BlackAndWhiteButtonClicked;
            this.colorDisplayWidget.SwapColorsClicked += ColorDisplayWidget_SwapColorsClicked;
            // 
            // ColorsForm
            // 
            this.ClientSize = new System.Drawing.Size(386, 666);
            this.Size = this.ClientSize;
            this.Controls.Add(this.colorDisplayWidget);
            this.Controls.Add(this.swatchControl);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "ColorsForm";
            this.Controls.SetChildIndex(this.swatchControl, 0);
            this.Controls.SetChildIndex(this.colorDisplayWidget, 0);
            this.ResumeLayout(false);
            this.PerformLayout();

        }
        #endregion

        public void SetUserColors(ColorBgra primary, ColorBgra secondary)
        {
            UserPrimaryColor = primary;
            UserSecondaryColor = secondary;
        }

        public void SwapUserColors()
        {
            ColorBgra primary = this.UserPrimaryColor;
            ColorBgra secondary = this.UserSecondaryColor;
            SetUserColors(secondary, primary);
        }

        public void SetUserColorsToBlackAndWhite()
        {
            SetUserColors(ColorBgra.Black, ColorBgra.White);
        }

        private void ColorDisplayWidget_SwapColorsClicked(object sender, EventArgs e)
        {
            SwapUserColors();
            OnRelinquishFocus();
        }

        private void ColorDisplayWidget_BlackAndWhiteButtonClicked(object sender, EventArgs e)
        {
            SetUserColorsToBlackAndWhite();
            OnRelinquishFocus();
        }

        private void SwatchControl_ColorClicked(object sender, EventArgs<Pair<int, MouseButtons>> e)
        {
            List<ColorBgra> colors = new List<ColorBgra>(this.swatchControl.Colors);

            ColorBgra color = colors[e.Data.First];

            if (e.Data.Second == MouseButtons.Right)
            {
                SetUserColors(UserPrimaryColor, color);
            }
            else
            {
                SetUserColors(color, userSecondaryColor);
            }

            OnRelinquishFocus();
        }

        private void OnPaletteClickedHandler(object sender, EventArgs e)
        {
            ToolStripItem tsi = sender as ToolStripItem;

            if (tsi != null)
            {
                ColorBgra[] palette = this.paletteCollection.Get(tsi.Text);

                if (palette != null)
                {
                    this.swatchControl.Colors = palette;
                }
            }
        }

        private void SwatchControl_ColorsChanged(object sender, EventArgs e)
        {
            string paletteString = PaletteCollection.GetPaletteSaveString(this.swatchControl.Colors);
            Settings.CurrentUser.SetString(SettingNames.CurrentPalette, paletteString);
        }
    }
}
