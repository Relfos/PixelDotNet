/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See src/Resources/Files/License.txt for full licensing and attribution      //
// details.                                                                    //
// .                                                                           //
/////////////////////////////////////////////////////////////////////////////////

using PixelDotNet.Actions;
using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace PixelDotNet.Menus
{
    internal sealed class ViewMenu
        : PdnMenuItem
    {
        private PdnMenuItem menuViewZoomIn;
        private PdnMenuItem menuViewZoomOut;
        private PdnMenuItem menuViewZoomToWindow;
        private PdnMenuItem menuViewZoomToSelection;
        private PdnMenuItem menuViewActualSize;
        private ToolStripSeparator menuViewSeparator1;
        private PdnMenuItem menuViewGrid;
        private ToolStripSeparator menuViewSeparator2;

        private bool OnOemPlusShortcut(Keys keys)
        {
            this.menuViewZoomIn.PerformClick();
            return true;
        }

        private bool OnOemMinusShortcut(Keys keys)
        {
            this.menuViewZoomOut.PerformClick();
            return true;
        }

        private bool OnCtrlAltZero(Keys keys)
        {
            this.menuViewActualSize.PerformClick();
            return true;
        }

        public ViewMenu()
        {
            InitializeComponent();
            PdnBaseForm.RegisterFormHotKey(Keys.Control | Keys.OemMinus, OnOemMinusShortcut);
            PdnBaseForm.RegisterFormHotKey(Keys.Control | Keys.Oemplus, OnOemPlusShortcut);
            PdnBaseForm.RegisterFormHotKey(Keys.Control | Keys.Alt | Keys.D0, OnCtrlAltZero);
        }

        private void InitializeComponent()
        {
            this.menuViewZoomIn = new PdnMenuItem();
            this.menuViewZoomOut = new PdnMenuItem();
            this.menuViewZoomToWindow = new PdnMenuItem();
            this.menuViewZoomToSelection = new PdnMenuItem();
            this.menuViewActualSize = new PdnMenuItem();
            this.menuViewSeparator1 = new ToolStripSeparator();
            this.menuViewGrid = new PdnMenuItem();
            this.menuViewSeparator2 = new ToolStripSeparator();
            // 
            // menuView
            // 
            this.DropDownItems.AddRange(
                new System.Windows.Forms.ToolStripItem[] 
                {
                    this.menuViewZoomIn,
                    this.menuViewZoomOut,
                    this.menuViewZoomToWindow,
                    this.menuViewZoomToSelection,
                    this.menuViewActualSize,
                    this.menuViewSeparator1,
                    this.menuViewGrid,
                    this.menuViewSeparator2,
                });
            this.Name = "Menu.View";
            this.Text = PdnResources.GetString("Menu.View.Text"); 
            // 
            // menuViewZoomIn
            // 
            this.menuViewZoomIn.Name = "ZoomIn";
            this.menuViewZoomIn.ShortcutKeys = Keys.Control | Keys.Add;
            this.menuViewZoomIn.ShortcutKeyDisplayString = PdnResources.GetString("Menu.View.ZoomIn.ShortcutKeyDisplayString");
            this.menuViewZoomIn.Click += new System.EventHandler(this.MenuViewZoomIn_Click);
            // 
            // menuViewZoomOut
            // 
            this.menuViewZoomOut.Name = "ZoomOut";
            this.menuViewZoomOut.ShortcutKeys = Keys.Control | Keys.Subtract;
            this.menuViewZoomOut.ShortcutKeyDisplayString = PdnResources.GetString("Menu.View.ZoomOut.ShortcutKeyDisplayString");
            this.menuViewZoomOut.Click += new System.EventHandler(this.MenuViewZoomOut_Click);
            // 
            // menuViewZoomToWindow
            // 
            this.menuViewZoomToWindow.Name = "ZoomToWindow";
            this.menuViewZoomToWindow.ShortcutKeys = Keys.Control | Keys.B;
            this.menuViewZoomToWindow.Click += new System.EventHandler(this.MenuViewZoomToWindow_Click);
            // 
            // menuViewZoomToSelection
            // 
            this.menuViewZoomToSelection.Name = "ZoomToSelection";
            this.menuViewZoomToSelection.ShortcutKeys = Keys.Control | Keys.Shift | Keys.B;
            this.menuViewZoomToSelection.Click += new System.EventHandler(this.MenuViewZoomToSelection_Click);
            // 
            // menuViewActualSize
            // 
            this.menuViewActualSize.Name = "ActualSize";
            this.menuViewActualSize.ShortcutKeys = Keys.Control | Keys.Shift | Keys.A;
            this.menuViewActualSize.Click += new System.EventHandler(this.MenuViewActualSize_Click);
            // 
            // menuViewGrid
            // 
            this.menuViewGrid.Name = "Grid";
            this.menuViewGrid.Click += new System.EventHandler(this.MenuViewGrid_Click);
        }

        protected override void OnDropDownOpening(EventArgs e)
        {
            if (AppWorkspace.ActiveDocumentWorkspace != null)
            {
                this.menuViewZoomIn.Enabled = true;
                this.menuViewZoomOut.Enabled = true;
                this.menuViewZoomToWindow.Enabled = true;
                this.menuViewZoomToSelection.Enabled = !AppWorkspace.ActiveDocumentWorkspace.Selection.IsEmpty;
                this.menuViewActualSize.Enabled = true;
                this.menuViewGrid.Enabled = true;

                this.menuViewZoomToWindow.Checked = (AppWorkspace.ActiveDocumentWorkspace.ZoomBasis == ZoomBasis.FitToWindow);
                this.menuViewGrid.Checked = AppWorkspace.ActiveDocumentWorkspace.DrawGrid;
            }
            else
            {
                this.menuViewZoomIn.Enabled = false;
                this.menuViewZoomOut.Enabled = false;
                this.menuViewZoomToWindow.Enabled = false;
                this.menuViewZoomToSelection.Enabled = false;
                this.menuViewActualSize.Enabled = false;
                this.menuViewGrid.Enabled = false;
            }

            base.OnDropDownOpening(e);
        }

        private void MenuViewZoomIn_Click(object sender, System.EventArgs e)
        {
            if (AppWorkspace.ActiveDocumentWorkspace != null)
            {
                AppWorkspace.ActiveDocumentWorkspace.PerformAction(new ZoomInAction());
            }
        }

        private void MenuViewZoomOut_Click(object sender, System.EventArgs e)
        {
            if (AppWorkspace.ActiveDocumentWorkspace != null)
            {
                AppWorkspace.ActiveDocumentWorkspace.PerformAction(new ZoomOutAction());
            }
        }

        private void MenuViewZoomToWindow_Click(object sender, EventArgs e)
        {
            if (AppWorkspace.ActiveDocumentWorkspace != null)
            {
                AppWorkspace.ActiveDocumentWorkspace.PerformAction(new ZoomToWindowAction());
            }
        }

        private void MenuViewZoomToSelection_Click(object sender, EventArgs e)
        {
            if (AppWorkspace.ActiveDocumentWorkspace != null)
            {
                AppWorkspace.ActiveDocumentWorkspace.PerformAction(new ZoomToSelectionAction());
            }
        }

        private void MenuViewActualSize_Click(object sender, System.EventArgs e)
        {
            if (AppWorkspace.ActiveDocumentWorkspace != null)
            {
                AppWorkspace.ActiveDocumentWorkspace.ZoomBasis = ZoomBasis.ScaleFactor;
                AppWorkspace.ActiveDocumentWorkspace.ScaleFactor = ScaleFactor.OneToOne;
            }
        }

        private void MenuViewGrid_Click(object sender, System.EventArgs e)
        {
            if (AppWorkspace.ActiveDocumentWorkspace != null)
            {
                AppWorkspace.ActiveDocumentWorkspace.DrawGrid = !AppWorkspace.ActiveDocumentWorkspace.DrawGrid;
            }
        }
    }
}
