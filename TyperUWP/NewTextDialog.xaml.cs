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
		public string TitleEntry
		{
			get => titleTb.Text;
			set => titleTb.Text = value;
		}
		
		public string TextEntry
		{
			get => textTb.Text;
			set => textTb.Text = value;
		}

		Texts textList;
		string editExisting;

		public NewTextDialog(Texts textList, bool edit)
		{
			this.InitializeComponent();
			this.textList = textList;
			if (edit)
				this.editExisting = textList.Current.Title;
		}
		
		private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
		{
			if (string.IsNullOrWhiteSpace(TitleEntry))
			{
				displayError(titleTb, "Title can't be empty");
				args.Cancel = true;
			}
			else if (string.IsNullOrWhiteSpace(TextEntry))
			{
				displayError(textTb, "Text can't be empty.");
				args.Cancel = true;
			}
			else if (editExisting != TitleEntry && textList.containsTitle(TitleEntry))
			{
				displayError(titleTb, "Another text with this title already exists.");
				args.Cancel = true;
			}
			else
			{
				if (!string.IsNullOrEmpty(editExisting))
					textList.remove(editExisting);
				textList.add(new TextEntry(TitleEntry, TextEntry));
			}
		}

		void displayError(TextBox entry, string error)
		{
			errorText.Text = error;
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
			textCharCount.Text = textTb.Text.Length + " characters";
		}
	}
}
