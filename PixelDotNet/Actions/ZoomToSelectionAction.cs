/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See src/Resources/Files/License.txt for full licensing and attribution      //
// details.                                                                    //
// .                                                                           //
/////////////////////////////////////////////////////////////////////////////////

using System;

namespace PixelDotNet.Actions
{
    internal sealed class ZoomToSelectionAction
        : DocumentWorkspaceAction
    {
        public override HistoryMemento PerformAction(DocumentWorkspace documentWorkspace)
        {
            documentWorkspace.ZoomToSelection();
            return null;
        }

        public ZoomToSelectionAction()
            : base(ActionFlags.KeepToolActive)
        {
        }
    }
}
