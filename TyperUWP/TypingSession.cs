using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace TyperUWP
{
	public class TypingSession : TyperLib.TypingSession
	{
		Panel textPanel;
		TextBlockEx[] writtenTextControls = new TextBlockEx[NumCharsFromCenter];
		TextBlockEx currentCharControl;
		TextBlock unwrittenTextControl;
		double spaceWidth;
		TypingSessionSettings settings = new TypingSessionSettings();
		public TypingSessionSettings Settings
		{
			get => settings;
			set
			{
				settings = value;
				settings.TypingSession = this;
				applyStyle();
			}
		}

		public TypingSession(Panel _textPanel, StackPanel writtenTextPanel, TextBlockEx _currentCharControl, TextBlock _unwrittenTextControl)
		{
			textPanel = _textPanel;
			for (int i = writtenTextControls.Length - 1; i >= 0; i--)
			{
				writtenTextControls[i] = new TextBlockEx();
				writtenTextPanel.Children.Add(writtenTextControls[i]);
			}
			
			currentCharControl = _currentCharControl;
			unwrittenTextControl = _unwrittenTextControl;
			settings.FontName = currentCharControl.FontFamily.Source;
			settings.FontSize = 30;
			settings.Background = Colors.Black;
			settings.Foreground = Color.FromArgb(255, 255, 255, 210);
			settings.ErrorForeground = Colors.Red;
			settings.CorrectForeground = Colors.Green;
			settings.TypingSession = this;
		}

		public void applyStyle()
		{
			//Set panel background
			textPanel.Background = settings.BackgroundBrush;
			
			//Written text font
			foreach (var control in writtenTextControls)
			{
				control.FontFamily = settings.FontFamily;
				control.FontSize = settings.FontSize; 
				control.Foreground = settings.ForegroundBrush;
			}

			//Current character
			currentCharControl.Background = settings.ForegroundBrush;
			currentCharControl.Foreground = settings.BackgroundBrush;
			currentCharControl.FontFamily = settings.FontFamily;
			currentCharControl.FontSize = settings.FontSize;

			//Set color of unwritten text
			unwrittenTextControl.Foreground = settings.ForegroundBrush;
			unwrittenTextControl.FontFamily = settings.FontFamily;
			unwrittenTextControl.FontSize = settings.FontSize;

			//Determine width of text blocks with just a space
			var tb = new TextBlock();
			tb.FontFamily = settings.FontFamily;
			tb.FontSize = settings.FontSize;
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
					control.Background = settings.BackgroundBrush;
					continue;
				}
				bool isCorrect = currentChar.Value.Item1;
				char c = currentChar.Value.Item2;
				if (c == ' ')
					control.Background = isCorrect ? settings.BackgroundBrush : settings.ErrorForegroundBrush;
				else
				{
					control.Foreground = isCorrect ? settings.CorrectForegroundBrush : settings.ErrorForegroundBrush;
					control.Background = settings.BackgroundBrush;
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

	public class TimeLimitConverter : Windows.UI.Xaml.Data.IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			var time = (TimeSpan)value;
			return time.Minutes.ToString("d2") + ":" + time.Seconds.ToString("d2");
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			var time = (string)value;
			return new TimeSpan(0, int.Parse(time.Substring(0, 2)), int.Parse(time.Substring(3, 2)));
		}
	}

	[Serializable]
	public class TypingSessionSettings : ISerializable
	{
		public static readonly Type[] SerializeTypes = new Type[] { typeof(Color) };
		public TypingSession TypingSession { get; set; }
		public string StartText { get; private set; }
		public bool Shuffle { get; set; } = true;
		
		public SolidColorBrush BackgroundBrush { get; private set; }
		public Color Background
		{
			get => BackgroundBrush.Color;
			set
			{
				BackgroundBrush = new SolidColorBrush(value);
				applyStyle();
			}
		}
		public SolidColorBrush ForegroundBrush { get; private set; }
		public Color Foreground
		{
			get => ForegroundBrush.Color;
			set
			{
				ForegroundBrush = new SolidColorBrush(value);
				applyStyle();
			}
		}
		public SolidColorBrush CorrectForegroundBrush { get; private set; }
		public Color CorrectForeground
		{
			get => CorrectForegroundBrush.Color;
			set
			{
				CorrectForegroundBrush = new SolidColorBrush(value);
				applyStyle();
			}
		}

		public SolidColorBrush ErrorForegroundBrush { get; private set; }
		public Color ErrorForeground
		{
			get => ErrorForegroundBrush.Color;
			set
			{
				ErrorForegroundBrush = new SolidColorBrush(value);
				applyStyle();
			}
		}

		double fontSize;
		public double FontSize
		{
			get => fontSize;
			set
			{
				fontSize = value;
				applyStyle();
			}
		}

		public FontFamily FontFamily { get; private set; }
		public string FontName
		{
			get => FontFamily.Source;
			set
			{
				FontFamily = new FontFamily(value);
				applyStyle();
			}
		}

		public TypingSessionSettings()
		{

		}

		public TypingSessionSettings(SerializationInfo info, StreamingContext context)
		{
			foreach (var entry in info)
			{
				if (entry.Name == "startText")
				{
					StartText = (string)entry.Value;
					Shuffle = StartText == null;
				}
				else if (entry.Name == "background")
					Background = (Color)entry.Value;
				else if (entry.Name == "foreground")
					Foreground = (Color)entry.Value;
				else if (entry.Name == "correctForeground")
					CorrectForeground = (Color)entry.Value;
				else if (entry.Name == "errorForeground")
					ErrorForeground = (Color)entry.Value;
				else if (entry.Name == "fontName")
					FontName = (string)entry.Value;
				else if (entry.Name == "fontSize")
					fontSize = (double)entry.Value;
			}
		}

		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("startText", Shuffle ? null : TypingSession.TextEntry.Title);
			info.AddValue("background", Background);
			info.AddValue("foreground", Foreground);
			info.AddValue("correctForeground", CorrectForeground);
			info.AddValue("errorForeground", ErrorForeground);
			info.AddValue("fontName", FontName);
			info.AddValue("fontSize", fontSize);
		}

		void applyStyle()
		{
			TypingSession?.applyStyle();
		}
	}
}
