// Fixer.cs
//
// Copyright 2014 Wave Software Limited.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

class Fixer
{
	private string m_Pathname = "";
	private XmlDocument m_Xml = null;
	private XmlNamespaceManager m_NamespaceManager = null;

	public Fixer(string pathname)
	{
		m_Pathname = pathname;
	}

	public void Fix()
	{
		LoadFile();
		ApplyFixes();
		SaveFile();
	}

	private void LoadFile()
	{
		m_Xml = new XmlDocument();
		m_Xml.Load(m_Pathname);

		m_NamespaceManager = new XmlNamespaceManager(m_Xml.NameTable);

		string[,] namespaces =
		{
			{ "ns5", "http://www.garmin.com/xmlschemas/ActivityGoals/v1" },
			{ "ns3", "http://www.garmin.com/xmlschemas/ActivityExtension/v2" },
			{ "ns2", "http://www.garmin.com/xmlschemas/UserProfile/v2" },
			{ "anon", "http://www.garmin.com/xmlschemas/TrainingCenterDatabase/v2" },
			{ "xsi", "http://www.w3.org/2001/XMLSchema-instance" },
			{ "ns4", "http://www.garmin.com/xmlschemas/ProfileExtension/v1" }
		};

		for (int iii = 0; iii < namespaces.Length / 2; ++iii)
		{
			m_NamespaceManager.AddNamespace(
				namespaces[iii, 0],
				namespaces[iii, 1]);
		}
	}

	private void SaveFile()
	{
		string dir = Path.GetDirectoryName(m_Pathname);
		string filename = Path.GetFileNameWithoutExtension(m_Pathname);
		string ext = Path.GetExtension(m_Pathname);
		string pathname =
			dir +
			Path.DirectorySeparatorChar +
			filename +
			"_fixfit" +
			ext;

		m_Xml.Save(pathname);
	}

	private void ApplyFixes()
	{
		FixZeroSpeed();
	}

	private void FixZeroSpeed()
	{
		AddDeltas();
		DeleteZeroSpeedTrackpoints();
		SetTrackpointsTime();
		SetTotalTimeSeconds();
		DeleteDeltas();
	}

	private void AddDeltas()
	{
		// Add a <Delta> node to each <Trackpoint> that records the time
		// (in seconds) betwee the N and N+1 <Trackpoint> nodes.

		string query = "//anon:Lap";
		
		XmlNodeList lap_nodes = m_Xml.SelectNodes(query, m_NamespaceManager);

		foreach (XmlNode lap_node in lap_nodes)
		{
			XmlNodeList trackpoint_nodes = lap_node.SelectNodes(
				"anon:Track/anon:Trackpoint",
				m_NamespaceManager);

			if (0 == trackpoint_nodes.Count)
				continue;

			for (int iii = 0; iii < trackpoint_nodes.Count - 1; ++iii)
			{
				XmlNode trackpoint_node = trackpoint_nodes[iii];
				XmlNode time_node =
					trackpoint_node.SelectSingleNode(
						"anon:Time",
						m_NamespaceManager);
				DateTime time =
					DateTime.Parse(time_node.ChildNodes[0].Value);
				XmlNode next_trackpoint_node = trackpoint_nodes[iii + 1];
				XmlNode next_time_node =
					next_trackpoint_node.SelectSingleNode(
						"anon:Time",
						m_NamespaceManager);
				DateTime next_time =
					DateTime.Parse(next_time_node.ChildNodes[0].Value);
				TimeSpan delta = next_time - time;
				XmlNode delta_node = XmlHlps.CreateNode(
					trackpoint_node,
					"Delta",
					"http://www.garmin.com/xmlschemas/TrainingCenterDatabase/v2");

				XmlHlps.SetValue(delta_node, (int)delta.TotalSeconds);
			}

			// Add a zero <Delta> to the last node.
			XmlNode last_trackpoint_node =
				trackpoint_nodes[trackpoint_nodes.Count - 1];
			XmlNode last_delta_node = XmlHlps.CreateNode(
				last_trackpoint_node,
				"Delta",
				"http://www.garmin.com/xmlschemas/TrainingCenterDatabase/v2");

			XmlHlps.SetValue(last_delta_node, 0);
		}
	}

	private void DeleteDeltas()
	{
		string query = "//anon:Lap";

		XmlNodeList lap_nodes = m_Xml.SelectNodes(query, m_NamespaceManager);

		foreach (XmlNode lap_node in lap_nodes)
		{
			XmlNodeList trackpoint_nodes = lap_node.SelectNodes(
				"anon:Track/anon:Trackpoint",
				m_NamespaceManager);

			foreach (XmlNode trackpoint_node in trackpoint_nodes)
			{
				XmlNode delta_node = trackpoint_node.SelectSingleNode(
					"anon:Delta",
					m_NamespaceManager);

				trackpoint_node.RemoveChild(delta_node);
			}
		}
	}

	private void DeleteZeroSpeedTrackpoints()
	{
		const double MIN_SPEED = 3.0;

		HashSet<XmlNode> nodes_to_remove = new HashSet<XmlNode>();
		string query = "//anon:Lap";
		XmlNodeList lap_nodes = m_Xml.SelectNodes(query, m_NamespaceManager);

		foreach (XmlNode lap_node in lap_nodes)
		{
			XmlNodeList trackpoint_nodes = lap_node.SelectNodes(
				"anon:Track/anon:Trackpoint",
				m_NamespaceManager);
			int iii = 0;

			while (iii < trackpoint_nodes.Count)
			{
				XmlNode trackpoint_node = trackpoint_nodes[iii];
				XmlNode time_node = trackpoint_node.SelectSingleNode(
					"anon:Time",
					m_NamespaceManager);
				DateTime time = DateTime.Parse(time_node.ChildNodes[0].Value);
				XmlNode speed_node = trackpoint_node.SelectSingleNode(
					"anon:Extensions/ns3:TPX/ns3:Speed",
					m_NamespaceManager);
				double speed = double.Parse(speed_node.ChildNodes[0].Value);

				if (speed < MIN_SPEED)
				{
					bool found_next_good = false;

					// Start search for good node from current bad node.
					int jjj = iii;

					while (!found_next_good && jjj < trackpoint_nodes.Count)
					{
						XmlNode next_trackpoint_node = trackpoint_nodes[jjj];
						XmlNode next_speed_node =
							next_trackpoint_node.SelectSingleNode(
								"anon:Extensions/ns3:TPX/ns3:Speed",
								m_NamespaceManager);
						double next_speed = double.Parse(
							next_speed_node.ChildNodes[0].Value);

						if (next_speed < MIN_SPEED)
							nodes_to_remove.Add(next_trackpoint_node);
						else
						{
							iii = jjj;
							found_next_good = true;
						}

						++jjj;
					}
				}

				++iii;
			}
		}

		Console.WriteLine(
			"Deleting: {0} zero speed trackpoints.",
			nodes_to_remove.Count);

		foreach (XmlNode node_to_remove in nodes_to_remove)
			node_to_remove.ParentNode.RemoveChild(node_to_remove);
	}

	private void SetTrackpointsTime()
	{
		string query = "//anon:Lap";

		XmlNodeList lap_nodes = m_Xml.SelectNodes(query, m_NamespaceManager);

		foreach (XmlNode lap_node in lap_nodes)
		{
			XmlNodeList trackpoint_nodes = lap_node.SelectNodes(
				"anon:Track/anon:Trackpoint",
				m_NamespaceManager);

			if (0 == trackpoint_nodes.Count)
				continue;

			XmlNode first_trackpoint_node = trackpoint_nodes[0];
			XmlNode first_time_node =
				first_trackpoint_node.SelectSingleNode(
					"anon:Time",
					m_NamespaceManager);
			DateTime time = DateTime.Parse(first_time_node.ChildNodes[0].Value);

			// Reset start of lap to first good <Trackpoint> node.
			XmlHlps.SetAttr(
				lap_node,
				"StartTime",
				time.ToUniversalTime().ToString("o"));

			XmlNode first_delta_node =
				first_trackpoint_node.SelectSingleNode(
					"anon:Delta",
					m_NamespaceManager);
			int delta = int.Parse(first_delta_node.ChildNodes[0].Value);

			for (int iii = 1; iii < trackpoint_nodes.Count; ++iii)
			{
				XmlNode trackpoint_node = trackpoint_nodes[iii];
				XmlNode time_node = trackpoint_node.SelectSingleNode(
					"anon:Time",
					m_NamespaceManager);

				time = time.AddSeconds(delta);

				XmlHlps.SetValue(time_node, time.ToUniversalTime().ToString("o"));

				XmlNode delta_node = trackpoint_node.SelectSingleNode(
					"anon:Delta",
					m_NamespaceManager);
				
				delta = int.Parse(delta_node.ChildNodes[0].Value);
			}
		}
	}

	private void SetTotalTimeSeconds()
	{
		string query = "//anon:Lap";

		XmlNodeList lap_nodes = m_Xml.SelectNodes(query, m_NamespaceManager);

		foreach (XmlNode lap_node in lap_nodes)
		{
			XmlNode total_time_seconds_node = lap_node.SelectSingleNode(
				"anon:TotalTimeSeconds",
				m_NamespaceManager);
			double total_time_seconds = double.Parse(
				total_time_seconds_node.ChildNodes[0].Value);
			XmlNodeList trackpoint_nodes = lap_node.SelectNodes(
				"anon:Track/anon:Trackpoint",
				m_NamespaceManager);
			XmlNode first_trackpoint_node = trackpoint_nodes[0];
			XmlNode first_time_node = first_trackpoint_node.SelectSingleNode(
				"anon:Time",
				m_NamespaceManager);
			DateTime first_time = DateTime.Parse(
				first_time_node.ChildNodes[0].Value);
			XmlNode last_trackpoint_node =
				trackpoint_nodes[trackpoint_nodes.Count - 1];
			XmlNode last_time_node = last_trackpoint_node.SelectSingleNode(
				"anon:Time",
				m_NamespaceManager);
			DateTime last_time = DateTime.Parse(
				last_time_node.ChildNodes[0].Value);
			TimeSpan delta = last_time - first_time;

			Console.WriteLine("Old total time: {0}s", total_time_seconds);
			Console.WriteLine("New total time: {0}s", delta.TotalSeconds);

			XmlHlps.SetValue(
				total_time_seconds_node,
				delta.TotalSeconds.ToString());
		}
	}
}
