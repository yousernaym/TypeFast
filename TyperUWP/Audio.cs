using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Media.Audio;
using Windows.Media.Capture;
using Windows.Media.Render;
using Windows.Storage;

namespace TyperUWP
{
	public class Audio : IDisposable
	{
		public enum Type { Fix, Error, Typing, Space, Backspace, Finished};
		AudioGraph audioGraph = null;
		AudioDeviceOutputNode deviceOutputNode = null;

		AudioFileInputNode fixNode = null;
		AudioFileInputNode errorNode = null;
		List<AudioFileInputNode> typingNodes = new List<AudioFileInputNode>();
		int lastTypingNodeIndex;
		List<AudioFileInputNode> spaceNodes = new List<AudioFileInputNode>();
		int lastSpaceNodeIndex;
		AudioFileInputNode backspaceNode = null;
		AudioFileInputNode finishedNode = null;
		
		Random random = new Random();
		public void Dispose()
		{
			if (fixNode != null)
				fixNode.Dispose();
			if (errorNode != null)
				errorNode.Dispose();
			foreach (var node in typingNodes)
			{
				if (node != null)
					node.Dispose();
			}
			foreach (var node in spaceNodes)
			{
				if (node != null)
					node.Dispose();
			}
			if (backspaceNode != null)
				backspaceNode.Dispose();
			if (finishedNode != null)
				finishedNode.Dispose();

			if (deviceOutputNode != null)
				deviceOutputNode.Dispose();
			if (audioGraph != null)
				audioGraph.Dispose();
		}
		public async Task init()
		{
			await createAudioGraph();
			if (audioGraph == null)
				return;
			await createDeviceOutputNode();
			if (deviceOutputNode == null)
				return;
			await createFileInputNodes();
			audioGraph.Start();
		}

		async Task createAudioGraph()
		{
			AudioGraphSettings settings = new AudioGraphSettings(AudioRenderCategory.SoundEffects);
			CreateAudioGraphResult result = await AudioGraph.CreateAsync(settings);
			if (result.Status == AudioGraphCreationStatus.Success)
				audioGraph = result.Graph;
		}

		async Task createDeviceOutputNode()
		{
			CreateAudioDeviceOutputNodeResult result = await audioGraph.CreateDeviceOutputNodeAsync();
			if (result.Status == AudioDeviceNodeCreationStatus.Success)
			{
				deviceOutputNode = result.DeviceOutputNode;
				deviceOutputNode.OutgoingGain = 1.35;
			}
			else
			{
				audioGraph.Dispose();
				audioGraph = null;
			}
		}

		async Task createFileInputNodes()
		{
			fixNode = await createFileInputNode("fix.wav");
			errorNode = await createFileInputNode("error.wav");
			typingNodes = await createFileInputNodesFromFolder("typing");
			spaceNodes = await createFileInputNodesFromFolder("space");
			backspaceNode = await createFileInputNode("backspace.wav");
			finishedNode = await createFileInputNode("finished.wav", 0.65);
		}

		async Task<AudioFileInputNode> createFileInputNode(string path, double gain = 1)
		{
			StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/Audio/" + path));
			CreateAudioFileInputNodeResult result = await audioGraph.CreateFileInputNodeAsync(file);
			if (result.Status == AudioFileNodeCreationStatus.Success)
			{
				result.FileInputNode.Stop();
				result.FileInputNode.AddOutgoingConnection(deviceOutputNode, gain);
				return result.FileInputNode;
			}
			else
				return null;
		}

		async Task<List<AudioFileInputNode>> createFileInputNodesFromFolder(string dir, double gain = 1)
		{
			var folder = await Package.Current.InstalledLocation.GetFolderAsync(Path.Combine("assets", "audio", dir));
			var files = await folder.GetFilesAsync();
			var nodes = new List<AudioFileInputNode>();
			foreach (var file in files)
			{
				var node = await createFileInputNode(Path.Combine(dir, file.Name), gain);
				nodes.Add(node);
			}
			return nodes;
		}

		public void play(Type type)
		{
			if (deviceOutputNode == null)
				return;
			if (type == Type.Fix)
				playNode(fixNode);
			else if (type == Type.Error)
				playNode(errorNode);
			else if (type == Type.Typing)
				playRandom(typingNodes, ref lastTypingNodeIndex);
			else if (type == Type.Space)
				playRandom(spaceNodes, ref lastSpaceNodeIndex);
			else if (type == Type.Backspace)
				playNode(backspaceNode);
			else if (type == Type.Finished)
				playNode(finishedNode);
		}

		void playNode(AudioFileInputNode node)
		{
			if (node == null)
				return;
			node.Reset();
			node.Start();
		}

		void playRandom(List<AudioFileInputNode> nodes, ref int lastIndex)
		{
			int index;
			if (nodes.Count == 1)
				index = 0;
			else
			{
				do
				{
					index = random.Next(0, nodes.Count);
				} while (index == lastIndex);
			}
			lastIndex = index;
			playNode(nodes[index]);
		}
	}
}
