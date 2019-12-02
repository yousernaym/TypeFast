using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
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
	public enum ListDirection { Up, Down, Auto };
	public sealed partial class ComboBoxEx : UserControl
	{
		new public Brush BorderBrush
		{
			get => textBox.BorderBrush;
			set
			{
				textBox.BorderBrush = value;
			}
		}
		new public double MinWidth
		{
			get => textBox.MinWidth;
			set
			{
				textBox.MinWidth = listPopup.MinWidth = list.MinWidth = value;
			}
		}

        new public double MaxWidth
        {
            get => textBox.MaxWidth;
            set
            {
                textBox.MaxWidth = listPopup.MaxWidth = list.MaxWidth = value;
            }
        }

        new public double Width
		{
			get => textBox.Width;
			set
			{
				textBox.Width = listPopup.Width = list.Width = value;
			}
		}

		public double MaxListHeight { get; set; } = 10000;
		
		new public Brush Background
		{
			get => textBox.Background;
			set
			{
				textBox.Background = value;
			}
		}

		public ListDirection ListDirection { get; set; }
		
		IEnumerable<string> itemSource;
		public IEnumerable<string> ItemSource
		{
			get => itemSource;
			set
			{
				itemSource = value;
				buildFilteredList("");
			}
		}

		string submittedItem;
		string selectedItem;
		public string SelectedItem
		{
			get => selectedItem == null ? "" : selectedItem;
			set
			{
				selectedItem = value;
				if (string.IsNullOrEmpty(value))
				{
					list.SelectedIndex = -1;
					setText("");
					textBox.PlaceholderText = "";
					submittedItem = null;
				}
				else
				{
					list.SelectedItem = value;
					list.ScrollIntoView(selectedItem, ScrollIntoViewAlignment.Default);
				}
			}
		}

		void setSelection(string value)
		{
			//if (string.IsNullOrEmpty(value))
			//	return;
			list.SelectionChanged -= List_SelectionChanged;
			SelectedItem = value;
			list.SelectionChanged += List_SelectionChanged;
		}

		public int SelectedIndex
		{
			get => list.SelectedIndex;
			set => SelectedItem = (string)list.Items[value];
		}

		public event EventHandler SelectionSubmitted;
		
		public ComboBoxEx()
		{
			this.InitializeComponent();
		}

		private void TextBox_GotFocus(object sender, RoutedEventArgs e)
		{
			listPopup.IsOpen = true;
			submittedItem = selectedItem;
			textBox.PlaceholderText = textBox.Text;
			setText("");
			buildFilteredList("");
		}

		void submit()
		{
			if (!listPopup.IsOpen)
				return;
			if (submittedItem == selectedItem)
				return;
			submittedItem = selectedItem;
			SelectionSubmitted?.Invoke(this, new EventArgs());
		}

		private void TextBox_LostFocus(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrEmpty(SelectedItem))
				selectedItem = submittedItem;
			else
				submit();
			listPopup.IsOpen = false;
			setText(SelectedItem);
		}

		private void buildFilteredList(string query)
		{
			int earliestMatchIndex = 1000;
			string earliestMatch = "";
			var matchingTexts = new LinkedList<string>();
			foreach (var item in ItemSource)
			{
				//var lowerItem = item.ToLower();
				int matchIndex;
				if ((matchIndex = item.IndexOf(query, StringComparison.OrdinalIgnoreCase)) >= 0)
				{
					if (earliestMatchIndex > matchIndex)
					{
						earliestMatchIndex = matchIndex;
						earliestMatch = item;
					}
					matchingTexts.AddLast(item);
				}
			}
			list.ItemsSource = matchingTexts;

			updatePopup();

			if (!string.IsNullOrEmpty(query) && !string.IsNullOrEmpty(earliestMatch))
				setSelection(earliestMatch);
			else if(!string.IsNullOrEmpty(selectedItem))
				setSelection(SelectedItem);
		}

		private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			buildFilteredList(textBox.Text);
		}

		private void TextBox_KeyDown(object sender, KeyRoutedEventArgs e)
		{
			if (e.Key == VirtualKey.Up)
			{
				if (SelectedIndex > 0)
					SelectedIndex--;
				else
					SelectedIndex = list.Items.Count - 1;
			}
			else if (e.Key == VirtualKey.Down)
			{
				if (SelectedIndex < list.Items.Count - 1)
					SelectedIndex++;
				else
					SelectedIndex = 0;
			}
			else if (e.Key == VirtualKey.Enter && !string.IsNullOrEmpty(SelectedItem))
				close();
			else if (e.Key == VirtualKey.Escape)
			{
				setSelection(submittedItem);
				close();
			}
		}

		void close()
		{
			list.Focus(FocusState.Programmatic);
		}

		private void setText(string text)
		{
			textBox.TextChanged -= TextBox_TextChanged;
			textBox.Text = text;
			textBox.TextChanged += TextBox_TextChanged;
		}

		private void List_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (list.SelectedIndex == -1)
				return;
			selectedItem = (string)list.SelectedItem;
			//submittedItem = textBox.PlaceholderText = SelectedItem;
			setText(SelectedItem);
			textBox.SelectionStart = textBox.Text.Length;
		}

		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			updatePopup();
		}

		void updatePopup()
		{
			list.Width = textBox.ActualWidth;

			var ttv = listPopup.TransformToVisual(Window.Current.Content);
			Point popupPos = ttv.TransformPoint(new Point(0, 0));
			var spaceAbove = popupPos.Y;
			var windowHeight = ((Frame)Window.Current.Content).ActualHeight;
			var spaceBelow = windowHeight - popupPos.Y - textBox.ActualHeight;

			var explicitDir = ListDirection;
			if (ListDirection == ListDirection.Auto)
				explicitDir = spaceAbove > spaceBelow ? ListDirection.Up : ListDirection.Down;
			if (explicitDir == ListDirection.Up)
			{
				listPopup.VerticalOffset = -list.ActualHeight;
				list.MaxHeight = spaceAbove;
			}
			else
			{
				listPopup.VerticalOffset = textBox.ActualHeight;
				list.MaxHeight = spaceBelow;
			}
			list.MaxHeight = Math.Min(list.MaxHeight, MaxListHeight); //Allows user to put a cap on list height
			list.UpdateLayout(); //Helps to fully scroll the curretly selected item into view.
		}

		private void ListPopup_Opened(object sender, object e)
		{
			//list.UpdateLayout();
			//listPopup.VerticalOffset = -list.ActualHeight;
			updatePopup();
			//textBox.CornerRadius = new CornerRadius(3, 3, 0, 0);
		}

		public void resetFilter()
		{
			list.ItemsSource = itemSource;
			setSelection(selectedItem);
		}

		private void UserControl_AccessKeyInvoked(UIElement sender, AccessKeyInvokedEventArgs args)
		{
			textBox.Focus(FocusState.Keyboard);
		}

		private void listPopup_Closed(object sender, object e)
		{
			//textBox.CornerRadius = new CornerRadius(3);
		}
	}
}
