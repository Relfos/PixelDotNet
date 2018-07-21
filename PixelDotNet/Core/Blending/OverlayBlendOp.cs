using System;

namespace PaintDotNet
{
    public sealed partial class UserBlendOps
	{
		public class OverlayBlendOp : UserBlendOp
		{
			public override ColorBgra Apply(ColorBgra lhs, ColorBgra rhs)
			{
				throw new NotImplementedException();
			}
		}
	}
}
