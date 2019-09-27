﻿using System;
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
	public class Audio
	{
		public enum Type { Fix, Error, Typing, Space, Backspace, Finished};
		AudioGraph audioGraph = null;
		AudioDeviceOutputNode deviceOutputNode = null;

		AudioFileInputNode fixNode = null;
		AudioFileInputNode errorNode = null;
		List<AudioFileInputNode> typingNodes = null;
		int lastTypingNodeIndex;
		List<AudioFileInputNode> spaceNodes = null;
		int lastSpaceNodeIndex;
		AudioFileInputNode backspaceNode = null;
		AudioFileInputNode finishedNode = null;
		
		Random random = new Random();

		public async Task init()
		{
			await createAudioGraph();
			await createDeviceOutputNode();
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
				deviceOutputNode.OutgoingGain = 1.4;
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
