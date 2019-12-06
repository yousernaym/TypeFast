using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
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
	[Serializable]
	public class TypingSession : TyperLib.TypingSession, ISerializable
	{
		public static readonly Type[] SerializeTypes = new Type[] { typeof(Color), typeof(TyperLib.TextEntry) };
		public TypingSessionView View { get; set; }
		public SolidColorBrush BackgroundBrush { get; private set; }
		public Color Background
		{
			get => BackgroundBrush.Color;
			set
			{
				BackgroundBrush = new SolidColorBrush(value);
				View?.applyStyle();
			}
		}
		public SolidColorBrush ForegroundBrush { get; private set; }
		public Color Foreground
		{
			get => ForegroundBrush.Color;
			set
			{
				ForegroundBrush = new SolidColorBrush(value);
				View?.applyStyle();
			}
		}
		public SolidColorBrush CorrectForegroundBrush { get; private set; }
		public Color CorrectForeground
		{
			get => CorrectForegroundBrush.Color;
			set
			{
				CorrectForegroundBrush = new SolidColorBrush(value);
				View?.applyStyle();
			}
		}

		public SolidColorBrush ErrorBackgroundBrush { get; private set; }
		public Color ErrorBackground
		{
			get => ErrorBackgroundBrush.Color;
			set
			{
				ErrorBackgroundBrush = new SolidColorBrush(value);
				View?.applyStyle();
			}
		}

		public SolidColorBrush ErrorForegroundBrush { get; private set; }
		public Color ErrorForeground
		{
			get => ErrorForegroundBrush.Color;
			set
			{
				ErrorForegroundBrush = new SolidColorBrush(value);
				View?.applyStyle();
			}
		}
		
		double fontSize;
		public double FontSize
		{
			get => fontSize;
			set
			{
				fontSize = value;
				View?.applyStyle();
			}
		}

		public FontFamily FontFamily { get; private set; }
		public string FontName
		{
			get => FontFamily.Source;
			set
			{
				FontFamily = new FontFamily(value);
				View?.applyStyle();
			}
		}

        bool hideWrittenChars;
        public bool HideWrittenChars
        {
            get => hideWrittenChars;
            set
            {
                hideWrittenChars = value;
                View?.draw();
			}
        }
		bool underlineCurrentChar;
		public bool UnderlineCurrentChar
		{
			get => underlineCurrentChar;
			set
			{
				underlineCurrentChar = value;
				View?.applyStyle();
			}
		}

		bool errorAudio;
		public bool ErrorAudio
		{
			get => errorAudio;
			set
			{
				errorAudio = value;
			}
		}
		bool typingAudio;
		public bool TypingAudio
		{
			get => typingAudio;
			set
			{
				typingAudio = value;
			}
		}

		public TypingSession()
		{
			FontName = "Verdana";
			FontSize = 35;
			Background = Colors.Black;
			Foreground = Color.FromArgb(255, 255, 255, 220);
			ErrorBackground = Colors.Red;
			ErrorForeground = Colors.White;
			CorrectForeground = Colors.Green;
			UnderlineCurrentChar = false;
			HideWrittenChars = false;
			ErrorAudio = true;
			TypingAudio = true;
		}

		protected TypingSession(SerializationInfo info, StreamingContext context) : base(info, context)
		{
			foreach (var entry in info)
			{
				if (entry.Name == "background")
					Background = (Color)entry.Value;
				else if (entry.Name == "foreground")
					Foreground = (Color)entry.Value;
				else if (entry.Name == "correctForeground")
					CorrectForeground = (Color)entry.Value;
				else if (entry.Name == "errorBackground")
					ErrorBackground = (Color)entry.Value;
				else if (entry.Name == "errorForeground")
					ErrorForeground = (Color)entry.Value;
				else if (entry.Name == "fontName")
					FontName = (string)entry.Value;
				else if (entry.Name == "fontSize")
					fontSize = (double)entry.Value;
				else if (entry.Name == "hideWrittenChars")
					HideWrittenChars = (bool)entry.Value;
				else if (entry.Name == "underlineCurrentChar")
					UnderlineCurrentChar = (bool)entry.Value;
				else if (entry.Name == "errorAudio")
					ErrorAudio = (bool)entry.Value;
				else if (entry.Name == "typingAudio")
					TypingAudio = (bool)entry.Value;
			}
		}

		override public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
			info.AddValue("background", Background);
			info.AddValue("foreground", Foreground);
			info.AddValue("correctForeground", CorrectForeground);
			info.AddValue("errorBackground", ErrorBackground);
			info.AddValue("errorForeground", ErrorForeground);
			info.AddValue("fontName", FontName);
			info.AddValue("fontSize", fontSize);
			info.AddValue("hideWrittenChars", hideWrittenChars);
			info.AddValue("underlineCurrentChar", underlineCurrentChar);
			info.AddValue("errorAudio", ErrorAudio);
			info.AddValue("typingAudio", TypingAudio);
		}
	}

	public class TypingSessionView
	{
		Panel textPanel;
		TextBlockEx[] writtenTextControls;
		TextBlockEx currentCharControl;
		TextBlock unwrittenTextControl;
		double spaceWidth;
		TypingSession session;
		public TypingSession Session
		{
			get => session;
			set
			{
				session = value;
				session.View = this;
				applyStyle();
			}
		}

        public TypingSessionView(Panel textPanel, StackPanel writtenTextPanel, TextBlockEx currentCharControl, TextBlock unwrittenTextControl, TypingSession session)
		{
			writtenTextControls = new TextBlockEx[TypingSession.NumCharsFromCenter];
			this.textPanel = textPanel;
			for (int i = writtenTextControls.Length - 1; i >= 0; i--)
			{
				writtenTextControls[i] = new TextBlockEx();
				writtenTextPanel.Children.Add(writtenTextControls[i]);
			}
			
			this.currentCharControl = currentCharControl;
            this.unwrittenTextControl = unwrittenTextControl;
			Session = session;
		}

		public void applyStyle()
		{
			//Set panel background
			textPanel.Background = Session.BackgroundBrush;
			
			//Written text font
			foreach (var control in writtenTextControls)
			{
				control.FontFamily = Session.FontFamily;
				control.FontSize = Session.FontSize; 
				control.Foreground = Session.ForegroundBrush;
                control.Background = Session.BackgroundBrush;
            }

            //Current character
            //currentCharControl.Background = Session.ForegroundBrush;
            //currentCharControl.ForeGround = Session.BackgroundBrush;
			if (Session.UnderlineCurrentChar)
			{
				currentCharControl.Background = Session.BackgroundBrush;
				currentCharControl.Foreground = Session.ForegroundBrush;
			}
			else
			{
				currentCharControl.Background = Session.ForegroundBrush;
				currentCharControl.Foreground = Session.BackgroundBrush;
			}
			currentCharControl.FontFamily = Session.FontFamily;
			currentCharControl.FontSize = Session.FontSize;
            currentCharControl.Underline = session.UnderlineCurrentChar;
            currentCharControl.updateHighContrastMarker(false);

            //Set color of unwritten text
            unwrittenTextControl.Foreground = Session.ForegroundBrush;
			unwrittenTextControl.FontFamily = Session.FontFamily;
			unwrittenTextControl.FontSize = Session.FontSize;

			//Determine width of text blocks with just a space
			var tb = new TextBlock();
			tb.FontFamily = Session.FontFamily;
			tb.FontSize = Session.FontSize;
			tb.Text = "a a";
			tb.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
			double a_a = tb.DesiredSize.Width;
			tb.Text = "a";
			tb.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
			double a = tb.DesiredSize.Width;
			spaceWidth = a_a - (a - 1) * 2;
			reset();
		}

		public void reset()
		{
			Session.reset();
			draw();
		}

		public void draw()
		{
			//Draw written chars
			var currentChar = Session.WrittenChars.First;
			foreach (var control in writtenTextControls)
			{
				if (currentChar == null)
				{
					control.Text = "";
					control.Background = Session.BackgroundBrush;
					control.updateHighContrastMarker(false);
					continue;
				}
				bool isCorrect = currentChar.Value.Correct;
				char c = currentChar.Value.Char;

                control.Background = isCorrect ? Session.BackgroundBrush : Session.ErrorBackgroundBrush;
                control.Foreground = isCorrect ? Session.CorrectForegroundBrush : Session.ErrorForegroundBrush;
                control.Text = c.ToString();

				if (c == ' ')
				{
					control.Width = spaceWidth;
				}
				else
				{
					if (Session.HideWrittenChars)
					{
						control.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
						control.Text = " ";
						control.Width = control.DesiredSize.Width;
					}
					else
						control.Width = double.NaN;
				}
				control.updateHighContrastMarker(Session.HideWrittenChars);
                currentChar = currentChar.Next;
			}
			if (string.IsNullOrEmpty(Session.UnwrittenTextToDraw))
			{
				unwrittenTextControl.Text = currentCharControl.Text = "";
			}
			else
			{
				char c = Session.UnwrittenTextToDraw[0];
              	if (c == ' ')
                {
					currentCharControl.Text = session.UnderlineCurrentChar ? "_" : " ";
                    currentCharControl.Width = spaceWidth;
                }
                else
                {
                    currentCharControl.Text = c.ToString();
                    currentCharControl.Width = double.NaN;
                }
                unwrittenTextControl.Text = Session.UnwrittenTextToDraw.Substring(1, Session.UnwrittenTextToDraw.Length - 1);
			}
            //currentCharControl.updateHighContrastMarker(false);
        }
		
	}

	public class TimeLimitConverter : Windows.UI.Xaml.Data.IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			var time = (TimeSpan)value;
			//if (time.TotalSeconds < 30)
			//	time = new TimeSpan(0, 0, 30);
			return time.Minutes.ToString("d2") + ":" + time.Seconds.ToString("d2");
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			var timeStr = (string)value;
			var timeSpan = new TimeSpan(0, int.Parse(timeStr.Substring(0, 2)), int.Parse(timeStr.Substring(3, 2)));
			//if (timeSpan.TotalSeconds < 30)
			//	timeSpan = new TimeSpan(0, 0, 30);
			return timeSpan;
		}
	}


}
