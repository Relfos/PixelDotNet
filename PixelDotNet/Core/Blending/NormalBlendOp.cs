using System;

namespace PaintDotNet
{
    public sealed partial class UserBlendOps
    {
		public class NormalBlendOp : UserBlendOp
		{
			public override ColorBgra Apply(ColorBgra lhs, ColorBgra rhs)
			{
				return lhs;
			}

            public static ColorBgra ApplyStatic(ColorBgra dstPixel, ColorBgra brushPixel)
            {
                throw new NotImplementedException();
            }
        }
	}
}
