using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;

namespace TyperLib
{
	using Chapters = SortedList<int, int>;
	static public class Bible
	{
		static HttpClient client = new HttpClient();
		static public SortedList<string, Chapters> Books { get; private set; }
				
		async static public void Init()
		{
			client.DefaultRequestHeaders.Add("api-key", "key");
			string booksResponse = await client.GetStringAsync(GetBooksUrl);
			string chaptersResponse = await client.GetStringAsync(GetChaptersUrl);
		}

		//public static string getRandomText(int numVerses)
		//{https://api.scripture.api.bible/v1/bibles/[id]/passages/PSA.1.1-PSA.1.2. 

		//}
	}
}