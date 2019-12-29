using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TyperLib
{
	using Records = List<Record>;

	[Serializable]
	internal class UserData : ISerializable
	{
		internal int SyncedWithVersion { get; set; }
		internal TextEntries TextEntries { get; set; } = new TextEntries();
		internal Records Records { get; set; } = new Records();
		internal GlobalStats GlobalStats = new GlobalStats();
		static readonly public Type[] SerializeTypes = new Type[] { typeof(TextEntry), typeof(Record), typeof(TyperLib.WordStats), typeof(TyperLib.GlobalStats), typeof(Dictionary<string, TyperLib.WordStats>) };

		internal UserData()
		{
		}

		public UserData(SerializationInfo info, StreamingContext context)
		{
			foreach (var entry in info)
			{
				if (entry.Name == "syncedWithVersion")
					SyncedWithVersion = (int)entry.Value;
				else if (entry.Name.StartsWith("textEntry_"))
						TextEntries.add((TextEntry)entry.Value);
				else if (entry.Name.StartsWith("record_"))
						Records.Add((Record)entry.Value);
				else if (entry.Name == "globalStats")
					GlobalStats = (GlobalStats)entry.Value;
			}
		}
		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("syncedWithVersion", Texts.Version);
			//Save individual texts and records in order to be able to shange data structure without changing file format.
			foreach (var textEntry in TextEntries)
				info.AddValue("textEntry_"+textEntry.Title, textEntry);
			for (int i = 0; i < Records.Count; i++)
				info.AddValue("record_"+i, Records[i]);
			info.AddValue("globalStats", GlobalStats);
		}
	}
}


//var assembly = IntrospectionExtensions.GetTypeInfo(typeof(LoadResourceText)).Assembly;
//Stream stream = assembly.GetManifestResourceStream("WorkingWithFiles.PCLTextResource.txt");
//string text = "";