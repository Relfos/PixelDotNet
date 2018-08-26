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
using System.Windows.Forms;

namespace PixelDotNet.Menus
{
    internal sealed class PdnMainMenu
        : MenuStripEx
    {
        private FileMenu fileMenu;
        private EditMenu editMenu;
        private ViewMenu viewMenu;
        private ImageMenu imageMenu;
        private LayersMenu layersMenu;
        private WindowMenu windowMenu;
        private HelpMenu helpMenu;
        private AppWorkspace appWorkspace;

        public AppWorkspace AppWorkspace
        {
            get
            {
                return this.appWorkspace;
            }

            set
            {
                this.appWorkspace = value;
                this.fileMenu.AppWorkspace = value;
                this.editMenu.AppWorkspace = value;
                this.viewMenu.AppWorkspace = value;
                this.imageMenu.AppWorkspace = value;
                this.layersMenu.AppWorkspace = value;
                this.windowMenu.AppWorkspace = value;
                this.helpMenu.AppWorkspace = value;
            }
        }

        public PdnMainMenu()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.fileMenu = new FileMenu();
            this.editMenu = new EditMenu();
            this.viewMenu = new ViewMenu();
            this.imageMenu = new ImageMenu();
            this.layersMenu = new LayersMenu();
            this.windowMenu = new WindowMenu();
            this.helpMenu = new HelpMenu();
            SuspendLayout();
            //
            // PdnMainMenu
            //
            this.Name = "PdnMainMenu";
            this.LayoutStyle = ToolStripLayoutStyle.HorizontalStackWithOverflow;
            this.Items.AddRange(
                new ToolStripItem[] 
                {
                    this.fileMenu,
                    this.editMenu,
                    this.viewMenu,
                    this.imageMenu,
                    this.layersMenu,
                    this.windowMenu,
                    this.helpMenu
                });
            ResumeLayout();
        }
    }
}
