using UnityEngine;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections;
using System;
using System.Collections.Generic;
using Assets.VoxelStation.Script;
using VoxelStation.Core;

public class VoxelStationData
{
    public double[] headPosition;
    public double stylus_Roll;

    public double[] penAtPt1;
    public double[] penAtPt2;

    public int keyType;
    public int isTrackingGlasses;
    public float screenAngle = 0.0f;

    public bool isRecievedData = false;

    private List<float> angleData;

    public VoxelStationData()
    {
        this.headPosition = new double[] { 0, 0, 0 };
        this.stylus_Roll = 0;
        this.penAtPt1 = new double[] { 0, 0, 0, 0 };
        this.penAtPt2 = new double[] { 0.1, 0, 0.1, 0 };

        this.keyType = 0;
        this.isTrackingGlasses = 0;
        this.screenAngle = 0.0f;

        this.angleData = new List<float>() { 0, 0, 0, 0, 0 };
    }

    void setAngle(float value)
    {
        if (value != angleData[4])
        {
            angleData.Add(value);
            angleData.RemoveAt(0);
        }
    }

    float getAngle()
    {
        return (angleData[0] + angleData[1] + angleData[2] + angleData[3] + angleData[4]) / 5;
    }

    public void SetData(string[] data)
    {
        this.headPosition = new double[] { double.Parse(data[0]), double.Parse(data[1]), double.Parse(data[2]) };

        this.stylus_Roll = double.Parse(data[3]);

        this.penAtPt1 = new double[] { double.Parse(data[4]), double.Parse(data[5]), double.Parse(data[6]), double.Parse(data[7]) };

        this.penAtPt2 = new double[] { double.Parse(data[8]), double.Parse(data[9]), double.Parse(data[10]), double.Parse(data[11]) };

        this.isTrackingGlasses = int.Parse(data[12]);

        setAngle(float.Parse(data[13]) );

        this.screenAngle = getAngle();

        isRecievedData = true;
    }
}

public sealed class UDPConnect
{
    static Char[] splitter = new Char[] { ',' };

    public string IP = "127.0.0.1";
    public int port = 8888;
	
	public delegate void StylusPress(int keyInfo);
    public static StylusPress stylusButtonPressed;
	
    private IPEndPoint ipEnd; //服务端端口
    private byte[] sendData = new byte[1024]; //发送的数据，必须为字节
    private Thread connectThread; //连接线程

    static Socket client;
    static int keyIn;

    public static string recvStr { get; set; }

	private static readonly UDPConnect instance = new UDPConnect();

	private UDPConnect() { }

	public static UDPConnect Instance
	{
		get
		{
			return instance;
		}
	}

    void SendBeat()
    {
		VoxelCore.Instance.StartCoroutine(SendAlive());
    }

    public void Startdevice()
    {
		UnityThread.initUnityThread();
		init();
		SendBeat();
    }

    public void StopDevice()
    {
        SocketSend("stop");

        connectThread.Abort();
        connectThread = null;

        client.Close();
        client = null;
    }

    public void SocketSend(string sendStr)
    {
        //清空发送缓存
        sendData = new byte[1024];
        //数据类型转换
        sendData = Encoding.ASCII.GetBytes(sendStr);
        //发送给指定服务端
        client.SendTo(sendData, sendData.Length, SocketFlags.None, ipEnd);
    }

    public void init()
    {
        ipEnd = new IPEndPoint(IPAddress.Parse(IP), port);

        client = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        client.Bind(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 0));

        string str = "start";
        sendMsg(str);
        connectThread = new Thread(ReciveMsg);
        connectThread.Start();
        stylusButtonPressed += UpdateKeyEvent;
    }

    public void UpdateKeyEvent(int keyInfo)
    {
		VoxelCore.Instance.voxelData.keyType = keyInfo;
        VoxelCore.Instance.UpdateButton(keyInfo);
    }

    /// <summary>  
    /// 向特定ip的主机的端口发送数据报  
    /// </summary>  
    static void sendMsg(string str)
    {
        EndPoint point = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8888);
        client.SendTo(Encoding.UTF8.GetBytes(str), point);
    }

    /// <summary>  
    /// 接收发送给本机ip对应端口号的数据报  
    /// </summary>  
    static void ReciveMsg()
    {
        while (true)
        {
            EndPoint point = new IPEndPoint(IPAddress.Any, 0);//用来保存发送方的ip和端口号  
            byte[] buffer = new byte[1024];
            int length = client.ReceiveFrom(buffer, ref point);//接收数据报  
            string message = Encoding.UTF8.GetString(buffer, 0, length);

            if (recvStr != null)
            {
                string[] s = recvStr.Split(splitter);
                if (s.Length <= 1)
                {
                    keyIn = (int)Char.Parse(s[0]);
					UnityThread.executeInUpdate(() =>
					{
						stylusButtonPressed(keyIn);
					});
                }

				if (s.Length > 1)
				{
					UnityThread.executeInUpdate(() =>
					{
						VoxelCore.Instance.voxelData.SetData(s);
						VoxelCore.Instance.voxelData.keyType = 0;
					});				
				}
			}
            recvStr = message;
        }
    }

    IEnumerator SendAlive()
    {
        SocketSend("live");
        yield return new WaitForSeconds(5);
		VoxelCore.Instance.StartCoroutine(SendAlive());
	}
}