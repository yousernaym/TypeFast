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

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace TyperUWP
{
	public sealed partial class NewTextDialog : ContentDialog
	{
		public string TitleEntry => titleTb.Text;
		public string TextEntry => textTb.Text;
		TextList textList;

		public NewTextDialog(TextList _textList)
		{
			this.InitializeComponent();
			textList = _textList;
		}
		
		private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
		{
		}

		private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
		{
		}

		private void ContentDialog_Closing(ContentDialog sender, ContentDialogClosingEventArgs args)
		{
			if (args.Result == ContentDialogResult.None)
				return;
			if (string.IsNullOrWhiteSpace(TitleEntry))
			{
				displayError(titleTb, "Title can't be empty");
				args.Cancel = true;
			}
			else if (textList.containsTitle(TitleEntry))
			{
				displayError(titleTb, "A text with this title already exists.");
				args.Cancel = true;
			}
			else if (string.IsNullOrWhiteSpace(TextEntry))
			{
				displayError(textTb, "Text can't be empty.");
				args.Cancel = true;
			}
			else
				textList.add(TitleEntry, TextEntry);
		}

		void displayError(TextBox entry, string error)
		{
			errorText.Text = error;
			errorFlyout.ShowAt(entry);
		}
	}
}
