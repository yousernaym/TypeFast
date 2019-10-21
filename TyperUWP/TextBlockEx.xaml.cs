using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
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

        string text;
		public string Text
		{
			get { return (string)GetValue(TextProperty); }
            set
            {
                SetValue(TextProperty, value);
                text = value;
                textBlock.TextDecorations = Underline ? Windows.UI.Text.TextDecorations.Underline : Windows.UI.Text.TextDecorations.None;
                textBlock.Text = value;
                //for (int i = 0; i < textBlock.Inlines.Count; i++)
                //{
                //    Inline inline = textBlock.Inlines[i];
                //    Run run;
                //    if (inline is Underline)
                //    {
                //        Underline underline = (Underline)inline;
                //        run = (Run)underline.Inlines[0];
                //       run.Text = Underline ? value : "";
                //    }
                //    else
                //    {
                //        run = (Run)inline;
                //        run.Text = Underline ? "" : value;
                //    }
                //}
            }
		}

		// Using a DependencyProperty as the backing store for Text.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty TextProperty =
			DependencyProperty.Register("Text", typeof(string), typeof(TextBlockEx), new PropertyMetadata(0));

        public bool Underline { get; set; } = false;

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
		new public static readonly DependencyProperty CornerRadiusProperty =
			DependencyProperty.Register("CornerRadius", typeof(CornerRadius), typeof(TextBlockEx), new PropertyMetadata(0));

		public TextBlockEx()
		{
			this.InitializeComponent();
		}

        public void updateHighContrastMarker(bool show)
        {
			if (show)
            { 
                highContrastMarker.Visibility = Visibility.Visible;
				//highContrastMarker.Width = border.Width;
                highContrastMarker.Stroke = border.Background;
                highContrastMarker.Fill = border.Background;
				//highContrastMarker.CenterPoint = border.CenterPoint;
				//highContrastMarker.Height = border.Height;
            }
            else
            {
                highContrastMarker.Visibility = Visibility.Collapsed;
            } 
        }
    }
}
