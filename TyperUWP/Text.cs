using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace TyperUWP
{
	class Text : TyperLib.Text
	{
		Panel textPanel;
		TextBlockEx[] writtenTextControls = new TextBlockEx[NumCharsFromCenter];
		TextBlockEx currentCharControl;
		TextBlock unwrittenTextControl;
		double spaceWidth;
		Brush backgroundBrush;
		Brush foregroundBrush;
		Brush correctBrush;
		Brush errorBrush;
		FontFamily fontFamily;
		double fontSize;

		public Text(Panel _textPanel, StackPanel writtenTextPanel, TextBlockEx _currentCharControl, TextBlock _unwrittenTextControl)
		{
			textPanel = _textPanel;
			for (int i = writtenTextControls.Length - 1; i >= 0; i--)
			{
				writtenTextControls[i] = new TextBlockEx();
				writtenTextPanel.Children.Add(writtenTextControls[i]);
			}
			
			currentCharControl = _currentCharControl;
			unwrittenTextControl = _unwrittenTextControl;
			setStyle("", 50, new SolidColorBrush(Colors.White), new SolidColorBrush(Colors.Black), new SolidColorBrush(Colors.Green), new SolidColorBrush(Colors.Red));
		}

		public void setStyle(string fontName, double _fontSize, Brush foreground, Brush background, Brush correct, Brush error)
		{
			//Create font family
			if (string.IsNullOrEmpty(fontName))
				fontFamily = currentCharControl.FontFamily;
			else
				fontFamily = new FontFamily(fontName);

			fontSize = _fontSize;

			//Assign brushes
			foregroundBrush = foreground;
			backgroundBrush = background;
			correctBrush = correct;
			errorBrush = error;

			//Set panel background
			textPanel.Background = background;
			
			//Written text style
			foreach (var control in writtenTextControls)
			{
				control.FontFamily = fontFamily;
				control.FontSize = fontSize;
				control.Foreground = foregroundBrush;
			}

			//Current character
			currentCharControl.Background = foregroundBrush;
			currentCharControl.Foreground = backgroundBrush;
			currentCharControl.FontFamily = fontFamily;
			currentCharControl.FontSize = fontSize;

			//Set color of unwritten text
			unwrittenTextControl.Foreground = foregroundBrush;
			unwrittenTextControl.FontFamily = fontFamily;
			unwrittenTextControl.FontSize = fontSize;

			//Determine width of text blocks with just a space
			var tb = new TextBlock();
			tb.FontFamily = fontFamily;
			tb.FontSize = fontSize;
			tb.Text = "a a";
			tb.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
			double a_a = tb.DesiredSize.Width;
			tb.Text = "a";
			tb.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
			double a = tb.DesiredSize.Width;
			spaceWidth = a_a - (a - 1) * 2;
			reset();
		}

		new public void reset()
		{
			base.reset();
			draw();
		}

		public void draw()
		{
			//Draw written chars
			var currentChar = writtenChars.First;
			foreach (var control in writtenTextControls)
			{
				if (currentChar == null)
				{
					control.Text = "";
					control.Background = backgroundBrush;
					continue;
				}
				bool isCorrect = currentChar.Value.Item1;
				char c = currentChar.Value.Item2;
				if (c == ' ')
					control.Background = isCorrect ? backgroundBrush : errorBrush;
				else
				{
					control.Foreground = isCorrect ? correctBrush : errorBrush;
					control.Background = backgroundBrush;
				}

					control.Text = c.ToString();
				control.Width = c == ' ' ? spaceWidth : Double.NaN;

				currentChar = currentChar.Next;
			}
			if (string.IsNullOrEmpty(unwrittenTextToDraw))
			{
				unwrittenTextControl.Text = currentCharControl.Text = "";
			}
			else
			{
				char c = unwrittenTextToDraw[0];
				currentCharControl.Text = c.ToString();
				currentCharControl.Width = c == ' ' ? spaceWidth : Double.NaN;
				unwrittenTextControl.Text = unwrittenTextToDraw.Substring(1, unwrittenTextToDraw.Length - 1);
			}
		}

	}
}
