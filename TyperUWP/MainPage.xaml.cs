using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Diagnostics;
using Windows.System;
using System.Threading;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.ViewManagement;
using Windows.Storage;
using TyperLib;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace TyperUWP
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class MainPage : Page
	{
		Text text;
		TextList textList = new TextList(ApplicationData.Current.LocalFolder.Path);
		private bool modalDialogOpen = false;

		public MainPage()
		{
			this.InitializeComponent();
			ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size(1000, 300));
			//ApplicationView.PreferredLaunchViewSize = new Size(1000, );
			//ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;
			Window.Current.CoreWindow.CharacterReceived += CoreWindow_CharacterReceived;
			text = new Text(textPanel, writtenTextPanel, currentCharControl, unwrittenTextControl);
			text.TimeChecked += Text_TimeChecked;
			//text.TheText = "abcdefghijklmnopqrstuvwxyzåäö";
			text.loadText();
			text.draw();
			updateTypingStats();
			textsCombo.Items.VectorChanged += textsCombo_Items_VectorChanged;

			foreach (var text in textList)
			{
				textsCombo.Items.Add(text.Title);
			}
			textsCombo.SelectedIndex = 0;
			//Clipboard.ContentChanged += async (s, e) =>
			//{
			//};
		}

		private void textsCombo_Items_VectorChanged(IObservableVector<object> sender, IVectorChangedEventArgs @event)
		{
			deleteTextBtn.IsEnabled = textsCombo.Items.Count > 0;
		}

		private async void Text_TimeChecked(object sender, EventArgs e)
		{
			await Dispatcher.RunAsync(CoreDispatcherPriority.High, delegate
			{
				timeText.Text = "Time\n" + text.RemainingTimeString;
				wpmText.Text = "WPM\n" + text.Wpm;
				accuracyText.Text = "Accuracy\n" + text.Accuracy.ToString("0.00") + " %";
			});
		}

		bool isKeyDown(VirtualKey key)
		{
			var state = CoreWindow.GetForCurrentThread().GetAsyncKeyState(key);
			return (state & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;
		}

		private bool isKeyLocked(VirtualKey key)
		{
			var state = CoreWindow.GetForCurrentThread().GetAsyncKeyState(key);
			return (state & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Locked;
		}

		private void CoreWindow_CharacterReceived(CoreWindow sender, CharacterReceivedEventArgs args)
		{
			if (modalDialogOpen || isKeyDown(VirtualKey.Control) || isKeyDown(VirtualKey.Menu))
				return;

			args.Handled = true; //needed?
			text.typeChar(args.KeyCode);
			text.draw();
			updateTypingStats();
		}

		private void updateTypingStats()
		{
			correctCharsText.Text = "Correct\n" + text.CorrectChars;
			incorrectCharsText.Text = "Incorrect\n" + text.IncorrectChars;
			fixedCharsText.Text = "Fixed\n" + text.FixedChars;
		}

		private void ResetBtn_Click(object sender, RoutedEventArgs e)
		{
			reset();
		}

		void reset()
		{
			if (modalDialogOpen)
				return;
			text.reset();
			updateTypingStats();
		}

		private void Page_Loaded(object sender, RoutedEventArgs e)
		{

		}

		async private void NewTextBtn_Click(object sender, RoutedEventArgs e)
		{
			if (modalDialogOpen)
				return;
			NewTextDialog newTextDialog = new NewTextDialog(textList);
			modalDialogOpen = true;
			ContentDialogResult result = await newTextDialog.ShowAsync();
			modalDialogOpen = false;

			if (result == ContentDialogResult.Primary)
			{
				textsCombo.Items.Add(newTextDialog.TitleEntry);
				textsCombo.SelectedIndex = textsCombo.Items.Count - 1;
			}
		}

		private void TextsCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (modalDialogOpen)
				return;
			string title = (string)textsCombo.SelectedItem;
			if (string.IsNullOrEmpty(title))
				return;
			text.TheText = textList.getText(title);
			reset();
		}

		async private void DeleteTextBtn_Click(object sender, RoutedEventArgs e)
		{
			if (modalDialogOpen)
				return;
			var dlg = new ContentDialog { PrimaryButtonText = "Yes", CloseButtonText = "No", Content = "Are you sure you want to delete this text? This action cannot be undone." };
			ContentDialogResult result = await dlg.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                textList.remove((string)textsCombo.SelectedItem);
                int selectedIndex = textsCombo.SelectedIndex;
                textsCombo.Items.RemoveAt(textsCombo.SelectedIndex);
                if (selectedIndex >= textsCombo.Items.Count)
                    selectedIndex--;
                textsCombo.SelectedIndex = selectedIndex;
            }
		}

		async private void TextCmPaste_Click(object sender, RoutedEventArgs e)
		{
			if (modalDialogOpen)
				return;
			DataPackageView dataPackageView = Clipboard.GetContent();
			if (dataPackageView.Contains(StandardDataFormats.Text))
			{
				text.TheText = await dataPackageView.GetTextAsync();
				reset();
				textsCombo.SelectedIndex = -1;
			}
		}
	}
}
