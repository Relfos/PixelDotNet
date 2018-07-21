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
    public class EventArgs<T>
        : EventArgs
    {
        private T data;
        public T Data
        {
            get
            {
                return data;
            }
        }

        public EventArgs(T data)
        {
            this.data = data;
        }
    }
}
