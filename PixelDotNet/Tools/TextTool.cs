/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See src/Resources/Files/License.txt for full licensing and attribution      //
// details.                                                                    //
// .                                                                           //
/////////////////////////////////////////////////////////////////////////////////

using PixelDotNet;
using PixelDotNet.HistoryMementos;
using PixelDotNet.SystemLayer;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Collections;
using System.ComponentModel;

namespace PixelDotNet.Tools
{
    internal class TextTool
        : Tool
    {
        private enum EditingMode
        {
            NotEditing,
            EmptyEdit,
            Editing
        }

        private string statusBarTextFormat = PdnResources.GetString("TextTool.StatusText.TextInfo.Format");
        private Point startMouseXY;
        private Point startClickPoint;
        private bool tracking;

        private MoveNubRenderer moveNub;
        private int ignoreRedraw;
        private RenderArgs ra;
        private EditingMode mode;
        private ArrayList lines;
        private int linePos;
        private int textPos;
        private Point clickPoint;
        private Font font;
        private TextAlignment alignment;
        private IrregularSurface saved;
        private const int cursorInterval = 300;
        private bool pulseEnabled;
        private System.DateTime startTime;
        private bool lastPulseCursorState;
        private Cursor textToolCursor;
        private PixelDotNet.Threading.ThreadPool threadPool;
        private bool enableNub = true;

        private CompoundHistoryMemento currentHA;

        private bool controlKeyDown = false;
        private DateTime controlKeyDownTime = DateTime.MinValue;
        private readonly TimeSpan controlKeyDownThreshold = new TimeSpan(0, 0, 0, 0, 400);

        private void AlphaBlendingChangedHandler(object sender, EventArgs e)
        {
            if (mode != EditingMode.NotEditing)
            {
                RedrawText(true);
            }
        }

        private EventHandler fontChangedDelegate;
        private void FontChangedHandler(object sender, EventArgs a)
        {
            font = AppEnvironment.FontInfo.CreateFont();
            if (mode != EditingMode.NotEditing)
            {
                this.sizes = null;
                RedrawText(true);
            }
        }

        private EventHandler fontSmoothingChangedDelegate;
        private void FontSmoothingChangedHandler(object sender, EventArgs e)
        {
            if (mode != EditingMode.NotEditing)
            {
                this.sizes = null;
                RedrawText(true);
            }
        }

        private EventHandler alignmentChangedDelegate;
        private void AlignmentChangedHandler(object sender, EventArgs a)
        {
            alignment = AppEnvironment.TextAlignment;
            if (mode != EditingMode.NotEditing)
            {
                this.sizes = null;
                RedrawText(true);
            }
        }

        private EventHandler brushChangedDelegate;
        private void BrushChangedHandler(object sender, EventArgs a)
        {
            if (mode != EditingMode.NotEditing)
            {
                RedrawText(true);
            }
        }

        private EventHandler antiAliasChangedDelegate;
        private void AntiAliasChangedHandler(object sender, EventArgs a)
        {
            if (mode != EditingMode.NotEditing)
            {
                this.sizes = null;
                RedrawText(true);
            }
        }

        private EventHandler foreColorChangedDelegate;
        private void ForeColorChangedHandler(object sender, EventArgs e)
        {
            if (mode != EditingMode.NotEditing)
            {
                RedrawText(true);
            }
        }

        private void BackColorChangedHandler(object sender, EventArgs e)
        {
            if (mode != EditingMode.NotEditing)
            {
                RedrawText(true);
            }
        }

        private bool OnBackspaceTyped(Keys keys)
        {
            if (!this.DocumentWorkspace.Visible)
            {
                return false;
            }
            else if (this.mode != EditingMode.NotEditing)
            {
                OnKeyPress(Keys.Back);
                return true;
            }
            else
            {
                return false;
            }
        }

        protected override void OnActivate()
        {
            PdnBaseForm.RegisterFormHotKey(Keys.Back, OnBackspaceTyped);

            base.OnActivate();

            this.textToolCursor = new Cursor(PdnResources.GetResourceStream("Cursors.TextToolCursor.cur"));
            this.Cursor = this.textToolCursor;

            fontChangedDelegate = new EventHandler(FontChangedHandler);
            fontSmoothingChangedDelegate = new EventHandler(FontSmoothingChangedHandler);
            alignmentChangedDelegate = new EventHandler(AlignmentChangedHandler);
            brushChangedDelegate = new EventHandler(BrushChangedHandler);
            antiAliasChangedDelegate = new EventHandler(AntiAliasChangedHandler);
            foreColorChangedDelegate = new EventHandler(ForeColorChangedHandler);

            ra = new RenderArgs(((BitmapLayer)ActiveLayer).Surface);
            mode = EditingMode.NotEditing;
            
            font = AppEnvironment.FontInfo.CreateFont();
            alignment = AppEnvironment.TextAlignment;

            AppEnvironment.BrushInfoChanged += brushChangedDelegate;
            AppEnvironment.FontInfoChanged += fontChangedDelegate;
            AppEnvironment.FontSmoothingChanged += fontSmoothingChangedDelegate;
            AppEnvironment.TextAlignmentChanged += alignmentChangedDelegate;
            AppEnvironment.PrimaryColorChanged += foreColorChangedDelegate;
            AppEnvironment.SecondaryColorChanged += new EventHandler(BackColorChangedHandler);
            AppEnvironment.AlphaBlendingChanged += new EventHandler(AlphaBlendingChangedHandler);
            
            this.threadPool = new PixelDotNet.Threading.ThreadPool();

            this.moveNub = new MoveNubRenderer(this.RendererList);
            this.moveNub.Shape = MoveNubShape.Compass;
            this.moveNub.Size = new SizeF(10, 10);
            this.moveNub.Visible = false;
            this.RendererList.Add(this.moveNub, false);
        }

        protected override void OnDeactivate()
        {
            PdnBaseForm.UnregisterFormHotKey(Keys.Back, OnBackspaceTyped);

            base.OnDeactivate();

            switch (mode)
            {
                case EditingMode.Editing: 
                    SaveHistoryMemento();    
                    break;

                case EditingMode.EmptyEdit: 
                    RedrawText(false); 
                    break;

                case EditingMode.NotEditing: 
                    break;

                default: 
                    throw new InvalidEnumArgumentException("Invalid Editing Mode");
            }

            if (ra != null)
            {
                ra.Dispose();
                ra = null;
            }

            if (saved != null)
            {
                saved.Dispose();
                saved = null;
            }

            AppEnvironment.BrushInfoChanged -= brushChangedDelegate;
            AppEnvironment.FontInfoChanged -= fontChangedDelegate;
            AppEnvironment.FontSmoothingChanged -= fontSmoothingChangedDelegate;
            AppEnvironment.TextAlignmentChanged -= alignmentChangedDelegate;
            AppEnvironment.PrimaryColorChanged -= foreColorChangedDelegate;
            AppEnvironment.SecondaryColorChanged -= new EventHandler(BackColorChangedHandler);
            AppEnvironment.AlphaBlendingChanged -= new EventHandler(AlphaBlendingChangedHandler);

            StopEditing();
            this.threadPool = null;

            this.RendererList.Remove(this.moveNub);
            this.moveNub.Dispose();
            this.moveNub = null;

            if (this.textToolCursor != null)
            {
                this.textToolCursor.Dispose();
                this.textToolCursor = null;
            }
        }

        private void StopEditing()
        {
            mode = EditingMode.NotEditing;
            pulseEnabled = false;
            lines = null;
            this.moveNub.Visible = false;
        }

        private void StartEditing()
        {
            this.linePos = 0;
            this.textPos = 0;
            this.lines = new ArrayList();
            this.sizes = null;
            this.lines.Add(string.Empty);
            this.startTime = DateTime.Now;
            this.mode = EditingMode.EmptyEdit;
            this.pulseEnabled = true;
            UpdateStatusText();
        }

        private void UpdateStatusText()
        {
            string text;
            ImageResource image;

            if (this.tracking)
            {
                text = GetStatusBarXYText();
                image = Image;
            }
            else
            {
                text = PdnResources.GetString("TextTool.StatusText.StartTyping");
                image = null;
            }

            SetStatus(image, text);
        }

        private void PerformEnter()
        {
            if (this.lines == null)
            {
                return;
            }

            string currentLine = (string)this.lines[this.linePos];

            if (this.textPos == currentLine.Length)
            {   
                // If we are at the end of a line, insert an empty line at the next line
                this.lines.Insert(this.linePos + 1, string.Empty);  
            }
            else
            {
                this.lines.Insert(this.linePos + 1, currentLine.Substring(textPos, currentLine.Length - this.textPos));
                this.lines[this.linePos] = ((string)this.lines[this.linePos]).Substring(0, this.textPos);
            }

            this.linePos++;
            this.textPos = 0;
            this.sizes = null;

        }

        private void PerformBackspace()
        {   
            if (textPos == 0 && linePos > 0)
            {
                int ntp = ((string)lines[linePos - 1]).Length;

                lines[linePos - 1] = ((string)lines[linePos - 1]) + ((string)lines[linePos]);
                lines.RemoveAt(linePos);
                linePos--;
                textPos = ntp;          
                sizes = null;
            }
            else if (textPos > 0)
            {
                string ln = (string)lines[linePos];

                // If we are at the end of a line, we don't need to place a compound string
                if (textPos == ln.Length)
                {
                    lines[linePos] = ln.Substring(0, ln.Length - 1);
                }
                else
                {
                    lines[linePos] = ln.Substring(0, textPos - 1) + ln.Substring(textPos);
                }                   

                textPos--;
                sizes = null;
            }
        }

        private void PerformControlBackspace()
        {
            if (textPos == 0 && linePos > 0)
            {
                PerformBackspace();
            }
            else if (textPos > 0)
            {
                string currentLine = (string)lines[linePos];
                int ntp = textPos;

                if (Char.IsLetterOrDigit(currentLine[ntp - 1]))
                {
                    while (ntp > 0 && (Char.IsLetterOrDigit(currentLine[ntp - 1])))
                    {
                        ntp--;
                    }
                }
                else if (Char.IsWhiteSpace(currentLine[ntp - 1]))
                {
                    while (ntp > 0 && (Char.IsWhiteSpace(currentLine[ntp - 1])))
                    {
                        ntp--;
                    }
                }
                else if (Char.IsPunctuation(currentLine[ntp - 1]))
                {
                    while (ntp > 0 && (Char.IsPunctuation(currentLine[ntp - 1])))
                    {
                        ntp--;
                    }
                }
                else
                {
                    ntp--;
                }

                lines[linePos] = currentLine.Substring(0, ntp) + currentLine.Substring(textPos);
                textPos = ntp;
                sizes = null;
            }
        }

        private void PerformDelete()
        {   
            // Where are we?!
            if ((linePos == lines.Count - 1) && (textPos == ((string)lines[lines.Count - 1]).Length))
            {   
                // If the cursor is at the end of the text block
                return;
            }
            else if (textPos == ((string)lines[linePos]).Length)
            {   
                // End of a line, must merge strings
                lines[linePos] = ((string)lines[linePos]) + ((string)lines[linePos + 1]);
                lines.RemoveAt(linePos + 1);
            }
            else 
            {   
                // Middle of a line somewhere
                lines[linePos] = ((string)lines[linePos]).Substring(0, textPos) + ((string)lines[linePos]).Substring(textPos + 1);
            }

            // Check for state change
            if (lines.Count == 1 && ((string)lines[0]) == "")
            {
                mode = EditingMode.EmptyEdit;
            }

            sizes = null;
        }

        private void PerformControlDelete()
        {
            // where are we?!
            if ((linePos == lines.Count - 1) && (textPos == ((string)lines[lines.Count - 1]).Length))
            {   
                // If the cursor is at the end of the text block
                return;
            }
            else if (textPos == ((string)lines[linePos]).Length)
            {   
                // End of a line, must merge strings
                lines[linePos] = ((string)lines[linePos]) + ((string)lines[linePos + 1]);
                lines.RemoveAt(linePos + 1);
            }
            else 
            {   
                // Middle of a line somewhere
                int ntp = textPos;
                string currentLine = (string)lines[linePos];

                if (Char.IsLetterOrDigit(currentLine[ntp]))
                {
                    while (ntp < currentLine.Length && (Char.IsLetterOrDigit(currentLine[ntp])))
                    {
                        currentLine = currentLine.Remove(ntp, 1);
                    }
                }
                else if (Char.IsWhiteSpace(currentLine[ntp]))
                {
                    while (ntp < currentLine.Length && (Char.IsWhiteSpace(currentLine[ntp])))
                    {
                        currentLine = currentLine.Remove(ntp, 1);
                    }
                }
                else if (Char.IsPunctuation(currentLine[ntp]))
                {
                    while (ntp < currentLine.Length && (Char.IsPunctuation(currentLine[ntp])))
                    {
                        currentLine = currentLine.Remove(ntp, 1);
                    }
                }
                else
                {
                    ntp--;
                }

                lines[linePos] = currentLine;
            }

            // Check for state change
            if (lines.Count == 1 && ((string)lines[0]) == "")
            {
                mode = EditingMode.EmptyEdit;
            }

            sizes = null;
        }

        private void PerformLeft()
        {
            if (textPos > 0)
            {
                textPos--;
            }
            else if (textPos == 0 && linePos > 0)
            {
                linePos--;
                textPos = ((string)lines[linePos]).Length;
            }
        }

        private void PerformControlLeft()
        {
            if (textPos > 0)
            {
                int ntp = textPos;
                string currentLine = (string)lines[linePos];

                if (Char.IsLetterOrDigit(currentLine[ntp - 1]))
                {
                    while (ntp > 0 && (Char.IsLetterOrDigit(currentLine[ntp - 1])))
                    {
                        ntp--;
                    }
                }
                else if (Char.IsWhiteSpace(currentLine[ntp - 1]))
                {
                    while (ntp > 0 && (Char.IsWhiteSpace(currentLine[ntp - 1])))
                    {
                        ntp--;
                    }
                }
                else if (ntp > 0 && Char.IsPunctuation(currentLine[ntp - 1]))
                {
                    while (ntp > 0 && Char.IsPunctuation(currentLine[ntp - 1]))
                    {
                        ntp--;
                    }
                }
                else
                {
                    ntp--;
                }

                textPos = ntp;
            }
            else if (textPos == 0 && linePos > 0)
            {
                linePos--;
                textPos = ((string)lines[linePos]).Length;
            }
        }

        private void PerformRight()
        {
            if (textPos < ((string)lines[linePos]).Length)
            {
                textPos++;
            }
            else if (textPos == ((string)lines[linePos]).Length && linePos < lines.Count - 1)
            {
                linePos++;
                textPos = 0;
            }
        }

        private void PerformControlRight()
        {
            if (textPos < ((string)lines[linePos]).Length)
            {
                int ntp = textPos;
                string currentLine = (string)lines[linePos];

                if (Char.IsLetterOrDigit(currentLine[ntp]))
                {
                    while (ntp < currentLine.Length && (Char.IsLetterOrDigit(currentLine[ntp])))
                    {
                        ntp++;
                    }
                }
                else if (Char.IsWhiteSpace(currentLine[ntp]))
                {
                    while (ntp < currentLine.Length && (Char.IsWhiteSpace(currentLine[ntp])))
                    {
                        ntp++;
                    }
                }
                else if (ntp > 0 && Char.IsPunctuation(currentLine[ntp]))
                {
                    while (ntp < currentLine.Length && Char.IsPunctuation(currentLine[ntp]))
                    {
                        ntp++;
                    }
                }
                else
                {
                    ntp++;
                }

                textPos = ntp;
            }
            else if (textPos == ((string)lines[linePos]).Length && linePos < lines.Count - 1)
            {
                linePos++;
                textPos = 0;
            }
        }

        private void PerformUp()
        {
            PointF p = TextPositionToPoint(new Position(linePos, textPos));
            p.Y -= this.sizes[0].Height; //font.Height;
            Position np = PointToTextPosition(p);
            linePos = np.Line;
            textPos = np.Offset;
        }

        private void PerformDown()
        {
            if (linePos == lines.Count - 1)
            {
                // last line -> don't do squat
            }
            else
            {
                PointF p = TextPositionToPoint(new Position(linePos, textPos));
                p.Y += this.sizes[0].Height; //font.Height;
                Position np = PointToTextPosition(p);
                linePos = np.Line;
                textPos = np.Offset;
            }
        }

        private Point GetUpperLeft(Size sz, int line)
        {
            Point p = clickPoint;
            p.Y = (int)(p.Y - (0.5 * sz.Height) + (line * sz.Height));

            switch (alignment)
            {
                case TextAlignment.Center:
                    p.X = (int)(p.X - (0.5) * sz.Width); 
                    break;

                case TextAlignment.Right: 
                    p.X = (int)(p.X - sz.Width);         
                    break;
            }

            return p;
        }

        private Size StringSize(string s)
        {
            // We measure using a 1x1 device context to avoid performance problems that arise otherwise with large images.
            using (Surface window = ScratchSurface.CreateWindow(new Rectangle(0, 0, 1, 1)))
            {
                using (RenderArgs ra2 = new RenderArgs(window))
                {
                    return SystemLayer.Fonts.MeasureString(
                        ra2.Graphics, 
                        this.font, 
                        s, 
                        false,
                        AppEnvironment.FontSmoothing);
                }
            }
        }

        private sealed class Position
        {
            private int line;
            public int Line
            {
                get
                {
                    return line;
                }

                set
                {
                    if (value >= 0)
                    {
                        line = value;
                    }
                    else
                    {
                        line = 0;
                    }
                }
            }

            private int offset;
            public int Offset
            {
                get
                {
                    return offset;
                }

                set
                {
                    if (value >= 0)
                    {
                        offset = value;
                    }
                    else
                    {
                        offset = 0;
                    }
                }
            }

            public Position(int line, int offset)
            {
                this.line = line;
                this.offset = offset;
            }
        }

        private void SaveHistoryMemento()
        {
            pulseEnabled = false;
            RedrawText(false);

            if (saved != null)
            {
                PdnRegion hitTest = Selection.CreateRegion();
                hitTest.Intersect(saved.Region);

                if (!hitTest.IsEmpty())
                {
                    BitmapHistoryMemento bha = new BitmapHistoryMemento(Name, Image, DocumentWorkspace, 
                        ActiveLayerIndex, saved);

                    if (this.currentHA == null)
                    {
                        HistoryStack.PushNewMemento(bha);
                    }
                    else
                    {
                        this.currentHA.PushNewAction(bha);
                        this.currentHA = null;
                    }
                }

                hitTest.Dispose();
                saved.Dispose();
                saved = null;
            }
        }

        private void DrawText(Surface dst, Font textFont, string text, Point pt, Size measuredSize, bool antiAliasing, Brush brush)
        {
            Rectangle dstRect = new Rectangle(pt, measuredSize);
            Rectangle dstRectClipped = Rectangle.Intersect(dstRect, ScratchSurface.Bounds);

            if (dstRectClipped.Width == 0 || dstRectClipped.Height == 0)
            {
                return;
            }

            using (Surface surface = new Surface(8, 8))
            {
                using (RenderArgs renderArgs = new RenderArgs(surface))
                {
                    renderArgs.Graphics.FillRectangle(brush, 0, 0, surface.Width, surface.Height);
                }

                DrawText(dst, textFont, text, pt, measuredSize, antiAliasing, surface);
            }
        }

        private unsafe void DrawText(Surface dst, Font textFont, string text, Point pt, Size measuredSize, bool antiAliasing, Surface brush8x8)
        {
            Point pt2 = pt;
            Size measuredSize2 = measuredSize;
            int offset = (int)textFont.Height;
            pt.X -= offset;
            measuredSize.Width += 2 * offset;
            Rectangle dstRect = new Rectangle(pt, measuredSize);
            Rectangle dstRectClipped = Rectangle.Intersect(dstRect, ScratchSurface.Bounds);

            if (dstRectClipped.Width == 0 || dstRectClipped.Height == 0)
            {
                return;
            }

            // We only use the first 8,8 of brush
            using (RenderArgs renderArgs = new RenderArgs(this.ScratchSurface))
            {
                renderArgs.Graphics.FillRectangle(Brushes.White, pt.X, pt.Y, measuredSize.Width, measuredSize.Height);

                if (measuredSize.Width > 0 && measuredSize.Height > 0)
                {
                    using (Surface s2 = renderArgs.Surface.CreateWindow(dstRectClipped))
                    {
                        using (RenderArgs renderArgs2 = new RenderArgs(s2))
                        {
                            SystemLayer.Fonts.DrawText(
                                renderArgs2.Graphics, 
                                this.font, 
                                text, 
                                new Point(dstRect.X - dstRectClipped.X + offset, dstRect.Y - dstRectClipped.Y),
                                false,
                                AppEnvironment.FontSmoothing);
                        }
                    }
                }

                // Mask out anything that isn't within the user's clip region (selected region)
                using (PdnRegion clip = Selection.CreateRegion())
                {
                    clip.Xor(renderArgs.Surface.Bounds); // invert
                    clip.Intersect(new Rectangle(pt, measuredSize));
                    renderArgs.Graphics.FillRegion(Brushes.White, clip.GetRegionReadOnly());
                }

                int skipX;

                if (pt.X < 0)
                {
                    skipX = -pt.X;
                }
                else
                {
                    skipX = 0;
                }

                int xEnd = Math.Min(dst.Width, pt.X + measuredSize.Width);

                bool blending = AppEnvironment.AlphaBlending;

                if (dst.IsColumnVisible(pt.X + skipX))
                {
                    for (int y = pt.Y; y < pt.Y + measuredSize.Height; ++y)
                    {
                        if (!dst.IsRowVisible(y))
                        {
                            continue;
                        }

                        ColorBgra *dstPtr = dst.GetPointAddressUnchecked(pt.X + skipX, y);
                        ColorBgra *srcPtr = ScratchSurface.GetPointAddress(pt.X + skipX, y);
                        ColorBgra *brushPtr = brush8x8.GetRowAddressUnchecked(y & 7);

                        for (int x = pt.X + skipX; x < xEnd; ++x)
                        {
                            ColorBgra srcPixel = *srcPtr;
                            ColorBgra dstPixel = *dstPtr;
                            ColorBgra brushPixel = brushPtr[x & 7];

                            int alpha = ((255 - srcPixel.R) * brushPixel.A) / 255; // we could use srcPixel.R, .G, or .B -- the choice here is arbitrary
                            brushPixel.A = (byte)alpha;

                            if (srcPtr->R == 255) // could use R, G, or B -- arbitrary choice
                            {
                                // do nothing -- leave dst alone
                            }
                            else if (alpha == 255 || !blending)
                            {
                                // copy it straight over
                                *dstPtr = brushPixel;
                            }
                            else
                            {
                                // do expensive blending
                                *dstPtr = UserBlendOps.NormalBlendOp.ApplyStatic(dstPixel, brushPixel);
                            }

                            ++dstPtr;
                            ++srcPtr;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Redraws the Text on the screen
        /// </summary>
        /// <remarks>
        /// assumes that the <b>font</b> and the <b>alignment</b> are already set
        /// </remarks>
        /// <param name="cursorOn"></param>
        private void RedrawText(bool cursorOn)
        {
            if (this.ignoreRedraw > 0)
            {
                return;
            }

            if (saved != null)
            {
                saved.Draw(ra.Surface); 
                ActiveLayer.Invalidate(saved.Region);
                saved.Dispose();
                saved = null;
            }

            // Save the Space behind the lines
            Rectangle[] rects = new Rectangle[lines.Count + 1];
            Point[] localUls = new Point[lines.Count];

            // All Lines
            bool recalcSizes = false;

            if (this.sizes == null)
            {
                recalcSizes = true;
                this.sizes = new Size[lines.Count + 1];
            }

            if (recalcSizes)
            {
                for (int i = 0; i < lines.Count; ++i)
                {
                    this.threadPool.QueueUserWorkItem(new System.Threading.WaitCallback(this.MeasureText), 
                        BoxedConstants.GetInt32(i));
                }

                this.threadPool.Drain();
            }

            for (int i = 0; i < lines.Count; ++i)
            {
                Point upperLeft = GetUpperLeft(sizes[i], i);
                localUls[i] = upperLeft;
                Rectangle rect = new Rectangle(upperLeft, sizes[i]);
                rects[i] = rect;
            }

            // The Cursor Line
            string cursorLine = ((string)lines[linePos]).Substring(0, textPos);
            Size cursorLineSize;
            Point cursorUL;
            Rectangle cursorRect;
            bool emptyCursorLineFlag;

            if (cursorLine.Length == 0)
            {
                emptyCursorLineFlag = true;
                Size fullLineSize = sizes[linePos];
                cursorLineSize = new Size(2, (int)(Math.Ceiling(font.GetHeight())));
                cursorUL = GetUpperLeft(fullLineSize, linePos);
                cursorRect = new Rectangle(cursorUL, cursorLineSize);
            }
            else if (cursorLine.Length == ((string)lines[linePos]).Length)
            {
                emptyCursorLineFlag = false;
                cursorLineSize = sizes[linePos];
                cursorUL = localUls[linePos];
                cursorRect = new Rectangle(cursorUL, cursorLineSize);
            }
            else
            {
                emptyCursorLineFlag = false;
                cursorLineSize = StringSize(cursorLine);
                cursorUL = localUls[linePos];
                cursorRect = new Rectangle(cursorUL, cursorLineSize);
            }

            rects[lines.Count] = cursorRect;

            // Account for overhang on italic or fancy fonts
            int offset = (int)this.font.Height;
            for (int i = 0; i < rects.Length; ++i)
            {
                rects[i].X -= offset;
                rects[i].Width += 2 * offset;
            }

            // Set the saved region
            using (PdnRegion reg = Utility.RectanglesToRegion(Utility.InflateRectangles(rects, 3)))
            {
                saved = new IrregularSurface(ra.Surface, reg);
            }

            // Draw the Lines
            this.uls = localUls;

            for (int i = 0; i < lines.Count; i++)
            {
                threadPool.QueueUserWorkItem(new System.Threading.WaitCallback(this.RenderText), BoxedConstants.GetInt32(i));
            }

            threadPool.Drain();

            // Draw the Cursor
            if (cursorOn)
            {           
                using (Pen cursorPen = new Pen(Color.FromArgb(255, AppEnvironment.PrimaryColor.ToColor()), 2))
                {
                    if (emptyCursorLineFlag)
                    {
                        ra.Graphics.FillRectangle(cursorPen.Brush, cursorRect);
                    }
                    else
                    {
                        ra.Graphics.DrawLine(cursorPen, new Point(cursorRect.Right, cursorRect.Top), new Point(cursorRect.Right, cursorRect.Bottom));
                    }
                }
            }

            PlaceMoveNub();
            UpdateStatusText();
            ActiveLayer.Invalidate(saved.Region);
            Update();
        }

        private string GetStatusBarXYText()
        {
            string unitsAbbreviationXY;
            string xString;
            string yString;

            Document.CoordinatesToStrings(AppWorkspace.Units, this.uls[0].X, this.uls[0].Y, out xString, out yString, out unitsAbbreviationXY);

            string statusBarText = string.Format(
                this.statusBarTextFormat,
                xString,
                unitsAbbreviationXY,
                yString,
                unitsAbbreviationXY);

            return statusBarText;
        }

        // Only used when measuring via background threads
        private void MeasureText(object lineNumberObj)
        {
            int lineNumber = (int)lineNumberObj;
            this.sizes[lineNumber] = StringSize((string)lines[lineNumber]);
        }

        // Only used when rendering via background threads
        private Point[] uls;
        private Size[] sizes;

        private void RenderText(object lineNumberObj)
        {
            int lineNumber = (int)lineNumberObj;

            using (Brush brush = AppEnvironment.CreateBrush(false))
            {
                DrawText(ra.Surface, this.font, (string)this.lines[lineNumber], this.uls[lineNumber], this.sizes[lineNumber], false, brush);
            }
        }

        private void PlaceMoveNub()
        {
            if (this.uls != null && this.uls.Length > 0)
            {
                Point pt = this.uls[uls.Length - 1];
                pt.X += this.sizes[uls.Length - 1].Width;
                pt.Y += this.sizes[uls.Length - 1].Height;
                pt.X += (int)(10.0 / DocumentWorkspace.ScaleFactor.Ratio);
                pt.Y += (int)(10.0 / DocumentWorkspace.ScaleFactor.Ratio);

                pt.X = (int)Math.Round(Math.Min(this.ra.Surface.Width - this.moveNub.Size.Width, pt.X));
                pt.X = (int)Math.Round(Math.Max(this.moveNub.Size.Width, pt.X));
                pt.Y = (int)Math.Round(Math.Min(this.ra.Surface.Height - this.moveNub.Size.Height, pt.Y));
                pt.Y = (int)Math.Round(Math.Max(this.moveNub.Size.Height, pt.Y));

                this.moveNub.Location = pt;
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Space:
                    if (mode != EditingMode.NotEditing)
                    {
                        // Prevent pan cursor from flicking to 'hand w/ the X' whenever use types a space in their text
                        e.Handled = true;
                    }
                    break;

                case Keys.ControlKey:
                    if (!this.controlKeyDown)
                    {
                        this.controlKeyDown = true;
                        this.controlKeyDownTime = DateTime.Now;
                    }
                    break;

                // Make sure these are not used to scroll the document around
                case Keys.Home | Keys.Shift:
                case Keys.Home:
                case Keys.End:
                case Keys.End | Keys.Shift:
                case Keys.Next | Keys.Shift:
                case Keys.Next:
                case Keys.Prior | Keys.Shift:
                case Keys.Prior:
                    if (this.mode != EditingMode.NotEditing)
                    {
                        OnKeyPress(e.KeyCode);
                        e.Handled = true;
                    }
                    break;

                case Keys.Tab:
                    if ((e.Modifiers & Keys.Control) == 0)
                    {
                        if (this.mode != EditingMode.NotEditing)
                        {
                            OnKeyPress(e.KeyCode);
                            e.Handled = true;
                        }
                    }
                    break;

                case Keys.Back:
                case Keys.Delete:
                    if (this.mode != EditingMode.NotEditing)
                    {
                        OnKeyPress(e.KeyCode);
                        e.Handled = true;
                    }
                    break;
            }

            // Ensure text is on screen when they are typing
            if (this.mode != EditingMode.NotEditing)
            {
                Point p = Point.Truncate(TextPositionToPoint(new Position(linePos, textPos)));
                Rectangle bounds = Utility.RoundRectangle(DocumentWorkspace.VisibleDocumentRectangleF);
                bounds.Inflate(-(int)font.Height, -(int)font.Height);

                if (!bounds.Contains(p))
                {
                    PointF newCenterPt = Utility.GetRectangleCenter((RectangleF)bounds);

                    // horizontally off
                    if (p.X > bounds.Right || p.Y < bounds.Left)
                    {
                        newCenterPt.X = p.X;
                    }
                
                    // vertically off
                    if (p.Y > bounds.Bottom || p.Y < bounds.Top)
                    {
                        newCenterPt.Y = p.Y;
                    }

                    DocumentWorkspace.DocumentCenterPointF = newCenterPt;
                }
            }

            base.OnKeyDown (e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.ControlKey:
                    TimeSpan heldDuration = (DateTime.Now - this.controlKeyDownTime);

                    // If the user taps Ctrl, then we should toggle the visiblity of the moveNub
                    if (heldDuration < this.controlKeyDownThreshold)
                    {
                        this.enableNub = !this.enableNub;
                    }

                    this.controlKeyDown = false;
                    break;
            }

            base.OnKeyUp(e);
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            switch (e.KeyChar)
            {
                case (char)13: // Enter
                    if (tracking)
                    {
                        e.Handled = true;
                    }
                    break;

                case (char)27: // Escape
                    if (tracking)
                    {
                        e.Handled = true;
                    }
                    else
                    {
                        if (mode == EditingMode.Editing)
                        {
                            SaveHistoryMemento();
                        }
                        else if (mode == EditingMode.EmptyEdit)
                        {
                            RedrawText(false);
                        }

                        if (mode != EditingMode.NotEditing)
                        {
                            e.Handled = true;
                            StopEditing();
                        }
                    }

                    break;
            }

            if (!e.Handled && mode != EditingMode.NotEditing && !tracking)
            {
                e.Handled = true;

                if (mode == EditingMode.EmptyEdit)
                {
                    mode = EditingMode.Editing;
                    CompoundHistoryMemento cha = new CompoundHistoryMemento(Name, Image, new List<HistoryMemento>());
                    this.currentHA = cha;
                    HistoryStack.PushNewMemento(cha);
                }

                if (!char.IsControl(e.KeyChar)) 
                {
                    InsertCharIntoString(e.KeyChar);
                    textPos++;
                    RedrawText(true);
                }
            }

            base.OnKeyPress (e);
        }

        protected override void OnKeyPress(Keys keyData)
        {
            bool keyHandled = true;
            Keys key = keyData & Keys.KeyCode;
            Keys modifier = keyData & Keys.Modifiers;

            if (tracking)
            {
                keyHandled = false;
            }
            else if (modifier == Keys.Alt)
            {
                // ignore so they can use Alt+#### to type special characters
            }
            else if (mode != EditingMode.NotEditing)
            {
                switch (key)
                {
                    case Keys.Back:
                        if (modifier == Keys.Control)
                        {
                            PerformControlBackspace();
                        }
                        else
                        {
                            PerformBackspace();
                        }

                        break;

                    case Keys.Delete:
                        if (modifier == Keys.Control)
                        {
                            PerformControlDelete();
                        }
                        else
                        {
                            PerformDelete();
                        }

                        break;

                    case Keys.Enter:
                        PerformEnter();
                        break;

                    case Keys.Left:
                        if (modifier == Keys.Control)
                        {
                            PerformControlLeft();
                        }
                        else
                        {
                            PerformLeft();
                        }

                        break;

                    case Keys.Right:
                        if (modifier == Keys.Control)
                        {
                            PerformControlRight();
                        }
                        else
                        {
                            PerformRight();
                        }

                        break;

                    case Keys.Up:
                        PerformUp();
                        break;

                    case Keys.Down:
                        PerformDown();
                        break;

                    case Keys.Home:
                        if (modifier == Keys.Control)
                        {
                            linePos = 0;
                        }

                        textPos = 0;
                        break;

                    case Keys.End:
                        if (modifier == Keys.Control)
                        {
                            linePos = lines.Count - 1;
                        }

                        textPos = ((string)lines[linePos]).Length;
                        break;

                    default:
                        keyHandled = false;
                        break;
                }

                this.startTime = DateTime.Now;

                if (this.mode != EditingMode.NotEditing && keyHandled)
                {
                    RedrawText(true);
                }
            }

            if (!keyHandled) 
            {
                base.OnKeyPress(keyData);
            }
        }

        private PointF TextPositionToPoint(Position p)
        {
            PointF pf = new PointF(0,0);

            Size sz = StringSize(((string)lines[p.Line]).Substring(0, p.Offset));
            Size fullSz = StringSize((string)lines[p.Line]);

            switch (alignment)
            {
                case TextAlignment.Left: 
                    pf = new PointF(clickPoint.X + sz.Width, clickPoint.Y + (sz.Height * p.Line));
                    break;

                case TextAlignment.Center: 
                    pf = new PointF(clickPoint.X + (sz.Width - (fullSz.Width/2)), clickPoint.Y + (sz.Height * p.Line));
                    break;

                case TextAlignment.Right: 
                    pf = new PointF(clickPoint.X + (sz.Width - fullSz.Width), clickPoint.Y + (sz.Height * p.Line));
                    break;
                    
                default: 
                    throw new InvalidEnumArgumentException("Invalid Alignment");
            }

            return pf;
        }

        private int FindOffsetPosition(float offset, string line, int lno)
        {
            for (int i = 0; i < line.Length; i++)
            {
                PointF pf = TextPositionToPoint(new Position(lno, i));
                float dx = pf.X - clickPoint.X;

                if (dx >= offset)
                {
                    return i;
                }
            }

            return line.Length;
        }

        private Position PointToTextPosition(PointF pf)
        {
            float dx = pf.X - clickPoint.X;
            float dy = pf.Y - clickPoint.Y;
            int line = (int)Math.Floor(dy / (float)this.sizes[0].Height);

            if (line < 0)
            {
                line = 0;
            }
            else if (line >= lines.Count)
            {
                line = lines.Count - 1;
            }

            int offset =  FindOffsetPosition(dx, (string)lines[line], line);
            Position p = new Position(line, offset);

            if (p.Offset >= ((string)lines[p.Line]).Length)
            {
                p.Offset = ((string)lines[p.Line]).Length;
            }

            return p;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (tracking)
            {
                Point newMouseXY = new Point(e.X, e.Y);
                Size delta = new Size(newMouseXY.X - startMouseXY.X, newMouseXY.Y - startMouseXY.Y);
                this.clickPoint = new Point(this.startClickPoint.X + delta.Width, this.startClickPoint.Y + delta.Height);
                RedrawText(false);
                UpdateStatusText();
            }
            else
            {
                bool touchingNub = this.moveNub.IsPointTouching(new Point(e.X, e.Y), false);

                if (touchingNub && this.moveNub.Visible)
                {
                    this.Cursor = this.handCursor;
                }
                else
                {
                    this.Cursor = this.textToolCursor;
                }
            }

            base.OnMouseMove (e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (tracking)
            {
                OnMouseMove(e);
                tracking = false;
                UpdateStatusText();
            }

            base.OnMouseUp (e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown (e);

            bool touchingMoveNub = this.moveNub.IsPointTouching(new Point(e.X, e.Y), false);

            if (this.mode != EditingMode.NotEditing && (e.Button == MouseButtons.Right || touchingMoveNub))
            {
                this.tracking = true;
                this.startMouseXY = new Point(e.X, e.Y);
                this.startClickPoint = this.clickPoint;
                this.Cursor = this.handCursorMouseDown;
                UpdateStatusText();
            }
            else if (e.Button == MouseButtons.Left)
            {
                if (saved != null)
                {
                    Rectangle bounds = Utility.GetRegionBounds(saved.Region);
                    bounds.Inflate(font.Height, font.Height);

                    if (lines != null && bounds.Contains(e.X, e.Y))
                    {
                        Position p = PointToTextPosition(new PointF(e.X, e.Y + (font.Height / 2)));
                        linePos = p.Line;
                        textPos = p.Offset;
                        RedrawText(true);
                        return;
                    }
                }

                switch (mode)
                {
                    case EditingMode.Editing:
                        SaveHistoryMemento();
                        StopEditing();
                        break;

                    case EditingMode.EmptyEdit:
                        RedrawText(false); 
                        StopEditing(); 
                        break;
                }

                clickPoint = new Point(e.X, e.Y);
                StartEditing();
                RedrawText(true);
            }
        }
    
        protected override void OnPulse()
        {
            base.OnPulse();

            if (!pulseEnabled)
            {
                return;
            }

            TimeSpan ts = (DateTime.Now - startTime);
            long ms = Utility.TicksToMs(ts.Ticks);
            
            bool pulseCursorState;

            if (0 == ((ms / cursorInterval) % 2))
            {
                pulseCursorState = true;
            }
            else
            {
                pulseCursorState = false;
            }

            pulseCursorState &= this.Focused;

            if (IsFormActive)
            {
                pulseCursorState &= ((ModifierKeys & Keys.Control) == 0);
            }

            if (pulseCursorState != lastPulseCursorState)
            {
                RedrawText(pulseCursorState);
                lastPulseCursorState = pulseCursorState;
            }

            if (IsFormActive && (ModifierKeys & Keys.Control) != 0) 
            {
                // hide the nub while Ctrl is held down
                this.moveNub.Visible = false;
            }
            else
            {
                this.moveNub.Visible = true;
            }

            // don't show the nub while the user is moving the text around
            this.moveNub.Visible &= !tracking;

            // don't show the nub when the user has tapped Ctrl
            this.moveNub.Visible &= this.enableNub;

            // Oscillate between 25% and 100% alpha over a period of 2 seconds
            // Alpha value of 100% is sustained for a large duration of this period
            const int period = 10000 * 2000; // 10000 ticks per ms, 2000ms per second
            long tick = ts.Ticks % period;
            double sin = Math.Sin(((double)tick / (double)period) * (2.0 * Math.PI));
            // sin is [-1, +1]

            sin = Math.Min(0.5, sin);
            // sin is [-1, +0.5]

            sin += 1.0;
            // sin is [0, 1.5]

            sin /= 2.0;
            // sin is [0, 0.75]

            sin += 0.25;
            // sin is [0.25, 1]

            if (this.moveNub != null)
            {
                int newAlpha = (int)(sin * 255.0);
                int clampedAlpha = Utility.Clamp(newAlpha, 0, 255);
                this.moveNub.Alpha = clampedAlpha;
            }

            PlaceMoveNub();
        }

        protected override void OnPasteQuery(IDataObject data, out bool canHandle)
        {
            base.OnPasteQuery(data, out canHandle);

            if (data.GetDataPresent(DataFormats.StringFormat, true) &&
                this.Active &&
                this.mode != EditingMode.NotEditing)
            {
                canHandle = true;
            }
        }

        protected override void OnPaste(IDataObject data, out bool handled)
        {
            base.OnPaste (data, out handled);

            if (data.GetDataPresent(DataFormats.StringFormat, true) &&
                this.Active &&
                this.mode != EditingMode.NotEditing)
            {
                ++this.ignoreRedraw;
                string text = (string)data.GetData(DataFormats.StringFormat, true);

                foreach (char c in text)
                {
                    if (c == '\n')
                    {
                        this.PerformEnter();
                    }
                    else
                    {
                        this.PerformKeyPress(new KeyPressEventArgs(c));
                    }
                }

                handled = true;
                --this.ignoreRedraw;

                this.moveNub.Visible = true;

                this.RedrawText(false);
            }
        }

        private void InsertCharIntoString(char c)
        {
            lines[linePos] = ((string)lines[linePos]).Insert(textPos, c.ToString());
            this.sizes = null;
        }

        public TextTool(DocumentWorkspace documentWorkspace)
            : base(documentWorkspace,
                   PdnResources.GetImageResource("Icons.TextToolIcon.png"),
                   PdnResources.GetString("TextTool.Name"),
                   PdnResources.GetString("TextTool.HelpText"),
                   't',
                   false,
                   ToolBarConfigItems.Brush | ToolBarConfigItems.Text | ToolBarConfigItems.AlphaBlending)
        {
        }
    }
}
