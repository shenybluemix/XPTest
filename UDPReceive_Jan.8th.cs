﻿using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices; 
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

// [NOTE] Use Marshal.
// Need to make sure the endianness of the UDP sender & receiver consistent.
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct XpPacket {
	[StructLayout(LayoutKind.Sequential)]
	public struct Vec3f {
		public float x;
		public float y;
		public float z;
	};

	[StructLayout(LayoutKind.Sequential)]
	// Hamiltonian Quaternion:
	public struct Vec4f {
		public float x;
		public float y;
		public float z;
		public float w;
	};

	[MarshalAsAttribute(UnmanagedType.I4)]
	public System.Int32 version;
	[MarshalAsAttribute(UnmanagedType.I4)]
	public System.Int32 length;
	[MarshalAsAttribute(UnmanagedType.I8)]
	public System.Int64 sendTs;  // reserved
	[MarshalAsAttribute(UnmanagedType.I8)]
	public System.Int64 recvTs;  // reserved
	[MarshalAsAttribute(UnmanagedType.I8)]
	public System.Int64 sensorTs;
	public Vec3f pos;
	public Vec4f rot;
	[MarshalAsAttribute(UnmanagedType.I4)]
	public System.Int32 trackingStatus;  // reserved
}
	
public class UDPReceive : MonoBehaviour {
	// Receiving Thread
	Thread receiveThread;

	// UdpClient object
	UdpClient client;

	public int port; // define > init

	// Infos
	public Vector3 pos_unity = Vector3.zero;
	public Quaternion quat_unity = Quaternion.identity;

	// If start from shell
	private static void Main() {
		UDPReceive receiveObj = new UDPReceive();
		receiveObj.init();

		string text="";
		do
		{
			text = System.Console.ReadLine();
		}
		while(!text.Equals("exit"));
	}

	// start from unity3d
	public void Start() {
		init();
	}

	// OnGUI
	void OnGUI() {
		Rect rectObj = new Rect(40, 10, 200, 400);
		GUIStyle style = new GUIStyle();
		style.alignment = TextAnchor.UpperLeft;
		GUI.Box(rectObj, "# UDPReceive listening " + port + " #\n"
			+ "xyz_unity: " + pos_unity.ToString() + "\n"
			+ "quat_unity:" + quat_unity.ToString() + "\n"
			+ "euler angles:" + quat_unity.eulerAngles.ToString()
			, style);
	}

	// Init
	private void init() {
		// define port: This has to be -udp_port xxxx for the xp tracking app
		port = 8888;
		print("UDPReceive.init()");
		print("Listen to port : " + port);

		receiveThread = new Thread(new ThreadStart(ReceiveData));
		receiveThread.IsBackground = true;
		receiveThread.Start();
	}

	// receive thread
	private void ReceiveData() {
		client = new UdpClient(port);  // receiving port
		while (true) {
			try {
				IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);  // sender
				byte[] data = client.Receive(ref anyIP);
				print("recevied udp pkt len = " + data.Length);

				GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
				XpPacket xp_pkt = (XpPacket)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(XpPacket));
				handle.Free();

				// Convert RFU (right-handed coordinate system) to RUF (left-handed coordinate system)
				pos_unity = new Vector3(xp_pkt.pos.x, xp_pkt.pos.z, xp_pkt.pos.y);
				quat_unity = new Quaternion(-xp_pkt.rot.x, -xp_pkt.rot.z, -xp_pkt.rot.y, xp_pkt.rot.w);

				// Re-orient the device coordinate to match Unity camera usage
				// {Device in XP}: RDF (righty) -> {Device in Unity}: RFD (lefty)
				// {Device in Unity}: RFD (lefty) -> {Camera in Unity}: RUF (lefty)
				quat_unity = quat_unity * Quaternion.AngleAxis(-90, Vector3.right);
			}
			catch (System.Exception err) {
				print(err.ToString());
			}
		}
	}

	// Update is called once per frame
	void Update () {
		transform.position = pos_unity;
		transform.rotation = quat_unity;
	}
}
