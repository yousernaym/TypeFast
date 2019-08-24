using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using TyperLib;
using System.Globalization;
using Windows.UI;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace TyperUWP
{
	public enum TextDialogType { New, Clone, Edit };
	public sealed partial class TextDialog : ContentDialog
	{
		public string TitleField
		{
			get => titleTb.Text;
			set => titleTb.Text = value;
		}
		
		public string TextField
		{
			get => textTb.Text;
			set => textTb.Text = value;
		}

		public bool AsciiLetters
		{
			get => (bool)asciiLettersCb.IsChecked;
			set => asciiLettersCb.IsChecked = value;
		}

		Texts texts;
		string editTitle = "";
		ComboBoxEx textsControl;

		public TextDialog(Texts texts, TypingSession typingSession, TextDialogType dialogType, ComboBoxEx textsControl)
		{
			this.InitializeComponent();
			this.texts = texts;
			this.textsControl = textsControl;
			if (dialogType != TextDialogType.New && texts.Current != null)
			{
				TitleField = texts.Current.Title;
				TextField = texts.Current.Text;
				AsciiLetters = texts.Current.AsciiLetters;
			}

			if (dialogType == TextDialogType.Edit)
			{
				Title = "Edit text";
				var notes = new TextBlock();
				notes.Text = "Editing a text will erase all associated high scores.";
				notes.Margin = new Thickness(0, 10, 0, 0);
				notes.Foreground = new SolidColorBrush(Colors.Yellow);
				stackPanel.Children.Add(notes);
				this.editTitle = texts.Current.Title;
			}
			else
			{
				Title = "Add new text";
				if (texts.Current != null && texts.Current.Text.Trim().StartsWith("__bible__", StringComparison.OrdinalIgnoreCase))
				{
					TitleField = typingSession.TextEntry.Title;
					TextField = typingSession.TextEntry.Text;
					AsciiLetters = texts.Current.AsciiLetters;
				}
			}
		}
		
		private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
		{
			TitleField = TitleField.Trim();
			TextField = TextField.Trim();
			if (string.IsNullOrWhiteSpace(TitleField))
			{
				displayError(titleTb, "Title can't be empty");
				args.Cancel = true;
			}
			else if (string.IsNullOrWhiteSpace(TextField))
			{
				displayError(textTb, "Text can't be empty.");
				args.Cancel = true;
			}
			else if (!editTitle.Equals(TitleField, StringComparison.OrdinalIgnoreCase) && texts.containsTitle(TitleField))
			{
				displayError(titleTb, "Another text with this title already exists.");
				args.Cancel = true;
			}
			else
			{
				if (!string.IsNullOrEmpty(editTitle))
					texts.remove(editTitle);
				texts.add(new TextEntry(TitleField, TextField, AsciiLetters));
				textsControl.ItemSource = texts.Titles;
			}
		}

		void displayError(TextBox entry, string error)
		{
			errorText.Text = error; //errorText is a TextBlock in errorFlyout
            //errorFlyout.Placement = FlyoutPlacementMode.Bottom;
            errorFlyout.ShowAt(entry);
		}

		private void ContentDialog_Opened(ContentDialog sender, ContentDialogOpenedEventArgs args)
		{
			updateCharCountText();
		}

		private void TextTb_TextChanged(object sender, TextChangedEventArgs e)
		{
			updateCharCountText();			
		}

		private void updateCharCountText()
		{
			string remainingChars = String.Format("{0:n0}", textTb.MaxLength - textTb.Text.Length).Replace(NumberFormatInfo.CurrentInfo.NumberGroupSeparator, " ");
			textCharCount.Text = $"{remainingChars} characters remaining";
		}

		private void TitleTb_GotFocus(object sender, RoutedEventArgs e)
		{
			//selectAllText((TextBox)sender);
		}

		private void TextTb_GotFocus(object sender, RoutedEventArgs e)
		{
			//selectAllText((TextBox)sender);
		}

		void selectAllText(TextBox tb)
		{
			tb.SelectionStart = 0;
			tb.SelectionLength = tb.Text.Length;
		}
	}
}
