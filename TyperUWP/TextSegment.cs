using System;
using Windows.UI.Xaml.Controls;

namespace TyperUWP
{
	internal class TextSegment
	{
		const int NumCharsFromCenter = TyperShared.Text.NumCharsFromCenter;
		TextBlock[] CharControls = new TextBlock[NumCharsFromCenter];

		internal void draw()
		{
			throw new NotImplementedException();
		}
	}
}