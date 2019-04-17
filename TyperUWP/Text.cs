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
	class Text : TyperShared.Text
	{
		Panel textPanel;
		TextBlock[] writtenTextControls = new TextBlock[NumCharsFromCenter];
		TextBlock currentCharControl;
		TextBlock unwrittenTextControl;
		Border currentCharBackground;
		double spaceWidth;

		public Text(Panel _textPanel, StackPanel writtenTextPanel, Border _currentCharBackground, TextBlock _currentCharControl, TextBlock _unwrittenTextControl)
		{
			textPanel = _textPanel;
			for (int i = writtenTextControls.Length - 1; i >= 0; i--)
			{
				writtenTextControls[i] = new TextBlock();
				writtenTextControls[i].Text = i.ToString();
				writtenTextPanel.Children.Add(writtenTextControls[i]);
				writtenTextControls[i].FontSize = 50;
			}
			
			currentCharBackground = _currentCharBackground;
			currentCharControl = _currentCharControl;
			unwrittenTextControl = _unwrittenTextControl;
			setStyle("", 50, Colors.White, Colors.Black);
		}

		public void setStyle(string fontName, int fontSize, Color foreGround, Color backGround)
		{
			//Create local variable containing font family
			FontFamily fontFamily;
			if (string.IsNullOrEmpty(fontName))
				fontFamily = currentCharControl.FontFamily;
			else
				fontFamily = new FontFamily(fontName);

			//Create local variables containing brushes
			var foreBrush = new SolidColorBrush(foreGround);
			var backBrush = new SolidColorBrush(backGround);

			//Set background
			textPanel.Background = backBrush;
			
			//Set color of written text
			foreach (var control in writtenTextControls)
			{
				control.FontFamily = fontFamily;
				control.FontSize = fontSize;
				control.Foreground = foreBrush;

			}

			//Set color and backgroung of current character
			currentCharBackground.Background = foreBrush;
			currentCharControl.Foreground = backBrush;
			currentCharControl.FontFamily = fontFamily;

			//Set color of unwritten text
			unwrittenTextControl.Foreground = foreBrush;
			unwrittenTextControl.FontFamily = fontFamily;

			//Determine width of text blocks with just a space
			var tb = new TextBlock();
			
			tb.Text = "a a";
			tb.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
			double a_a = tb.DesiredSize.Width;
			tb.Text = "a";
			tb.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
			double a = tb.DesiredSize.Width;
			spaceWidth = a_a - (a - 1) * 2;
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
					continue;
				}
				bool isCorrect = currentChar.Value.Item1;
				char c = currentChar.Value.Item2;
				control.Foreground = new SolidColorBrush(isCorrect ? Colors.Green : Colors.Red);
				control.Text = c.ToString();
				control.Width = c == ' ' ? spaceWidth : Double.NaN;
				
				//control.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
				//control.Width = control.DesiredSize.Width;
				currentChar = currentChar.Next;
			}
			if (!string.IsNullOrEmpty(unwrittenTextToDraw))
			{
				char c = unwrittenTextToDraw[0];
				currentCharControl.Text = c.ToString();
				currentCharControl.Width = c == ' ' ? spaceWidth : Double.NaN;
				
				//currentCharControl.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
				//currentCharControl.Width = currentCharBackground.Width = currentCharControl.DesiredSize.Width;
				unwrittenTextControl.Text = unwrittenTextToDraw.Substring(1, unwrittenTextToDraw.Length - 1);
			}
		}

	}
}
