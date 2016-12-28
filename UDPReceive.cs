using System.Collections;
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
	public string pos_str = "";
	public string quat_str = "";

	//position and rotation for Camera
	public float posX;
	public float posY;
	public float posZ;

	public float rotX;
	public float rotY;
	public float rotZ;
	public float rotW;

	//Initial position for Camera deault in Unity is (0,1,-10)
	public float CaminitPosX = 0;
	public float CaminitPosY = 0;
	public float CaminitPosZ = 0;


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
			+ "xyz: " + pos_str + "\n"
			+ "quat:" + quat_str + "\n"
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
			
				pos_str = xp_pkt.pos.x + " " + xp_pkt.pos.y + " " + xp_pkt.pos.z;
				quat_str = xp_pkt.rot.x + " " + xp_pkt.rot.y + " " + xp_pkt.rot.z + " " + xp_pkt.rot.w;


				//Set the position X,Y,Z variable 
				
				posX = xp_pkt.pos.x;
				posY = xp_pkt.pos.z;
				posZ = xp_pkt.pos.y;

				// ?? how to set pkt.rot.x / y/ z to 
				rotX = xp_pkt.rot.x;
				rotY = xp_pkt.rot.y;
				rotZ = xp_pkt.rot.z;
				rotW = xp_pkt.rot.w;

			}
			catch (System.Exception err) {
				print(err.ToString());
			}
		}
	}

	// Update is called once per frame
	void Update () {
		//transform.position = new Vector3(posX + CaminitPosX,posY + CaminitPosY, posZ + CaminitPosZ);
		
		//Convert Camera Quaternion to Unity Quaternion
		Quaternion quat = new Quaternion(rotX,rotZ,rotY,rotW);
		Vector3 euler = quat.eulerAngles;
		
		print (" Chris:" + euler.x + "               " + euler.y + "            " + euler.z);

		//Check this eulerAngles
		transform.eulerAngles = new Vector3 (  (euler.x + 90) , euler.y, euler.z );
	}
}
