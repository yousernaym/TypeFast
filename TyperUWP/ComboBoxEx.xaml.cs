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

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace TyperUWP
{
	public sealed partial class ComboBoxEx : UserControl
	{
		IEnumerable<string> itemSource;
		public IEnumerable<string> ItemSource
		{
			get => itemSource;
			set
			{
				itemSource = value;
				buildFilteredList();
			}
		}

		public ComboBoxEx()
		{
			this.InitializeComponent();
		}

		private void TextBox_GotFocus(object sender, RoutedEventArgs e)
		{
			buildFilteredList();
			Flyout.ShowAttachedFlyout(textBox);
		}

		private void buildFilteredList()
		{
				var matchingTexts = new LinkedList<string>();
				foreach (var item in ItemSource)
				{
					if (item.ToLower().Contains(textBox.Text.ToLower()))
						matchingTexts.AddFirst(item);
				}
				if (matchingTexts.Count == 0)
					matchingTexts.AddFirst("No matching titles found.");
				list.ItemsSource = matchingTexts;
			
		}

		private void TextBox_LostFocus(object sender, RoutedEventArgs e)
		{

		}

		private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			buildFilteredList();
		}
	}
}
