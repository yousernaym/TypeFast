using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace TyperUWP
{
	public sealed partial class TextBlockEx : UserControl
	{
		new public Thickness Margin
		{
			get { return border.Margin; }
			set
			{
				border.Margin = value;
				SetValue(MarginProperty, value);
			}
		}

		// Using a DependencyProperty as the backing store for Margin.  This enables animation, styling, binding, etc...
		new public static readonly DependencyProperty MarginProperty =
			DependencyProperty.Register("Margin", typeof(Thickness), typeof(TextBlockEx), new PropertyMetadata(0));

		new public Thickness Padding
		{
			get { return border.Padding; }
			set
			{
				border.Padding = value;
				SetValue(PaddingProperty, value);
			}
		}

		// Using a DependencyProperty as the backing store for Padding.  This enables animation, styling, binding, etc...
		new public static readonly DependencyProperty PaddingProperty =
			DependencyProperty.Register("Padding", typeof(Thickness), typeof(TextBlockEx), new PropertyMetadata(0));
				
		public Brush ForeGround
		{
			get { return textBlock.Foreground; }
			set
			{
				SetValue(ForeGroundProperty, value);
				textBlock.Foreground = value;
			}
		}
		// Using a DependencyProperty as the backing store for ForeGround.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ForeGroundProperty =
			DependencyProperty.Register("ForeGround", typeof(Brush), typeof(TextBlockEx), new PropertyMetadata(0));

		new public Brush Background
		{
			get { return border.Background; }
			set
			{
				SetValue(BackgroundProperty, value);
				border.Background = value;
			}
		}
		// Using a DependencyProperty as the backing store for Background.  This enables animation, styling, binding, etc...
		new public static readonly DependencyProperty BackgroundProperty =
			DependencyProperty.Register("Background", typeof(Brush), typeof(TextBlockEx), new PropertyMetadata(0));

		public string Text
		{
			get { return (string)GetValue(TextProperty); }
			set
			{
				SetValue(TextProperty, value);
				textBlock.Text = value;
			}
		}

		// Using a DependencyProperty as the backing store for Text.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty TextProperty =
			DependencyProperty.Register("Text", typeof(string), typeof(TextBlockEx), new PropertyMetadata(0));

		new public CornerRadius CornerRadius
		{
			get { return border.CornerRadius; }
			set
			{
				border.CornerRadius = value;
				SetValue(CornerRadiusProperty, value);
			}
		}

		// Using a DependencyProperty as the backing store for CornerRadius.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty CornerRadiusProperty =
			DependencyProperty.Register("CornerRadius", typeof(CornerRadius), typeof(TextBlockEx), new PropertyMetadata(0));



		public TextBlockEx()
		{
			this.InitializeComponent();
		}
	}
}
