// taken from https://forum.unity.com/threads/simple-udp-implementation-send-read-via-mono-c.15900/#post-3645256

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class UdpConnection
{
  private UdpClient udpClient;

  private readonly Queue<byte[]> incomingQueue = new Queue<byte[]>();
  Thread receiveThread;
  private bool threadRunning = false;
  private string senderIp;
  private int senderPort;

  public void StartConnection(string sendIp, int sendPort, int receivePort)
  {
    try { udpClient = new UdpClient(); }
    catch (Exception e)
    {
      Debug.Log("Failed to listen for UDP at port " + receivePort + ": " + e.Message);
      return;
    }
    Debug.Log("Created receiving client at ip  and port " + receivePort);
    this.senderIp = sendIp;
    this.senderPort = sendPort;

    Debug.Log("Set sendee at ip " + sendIp + " and port " + sendPort);

    StartReceiveThread();
  }

  private void StartReceiveThread()
  {
    receiveThread = new Thread(() => ListenForMessages(udpClient));
    receiveThread.IsBackground = true;
    threadRunning = true;
    receiveThread.Start();
  }

  private void ListenForMessages(UdpClient client)
  {
    IPEndPoint remoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);

    while (threadRunning)
    {
      try
      {
        Byte[] receiveBytes = client.Receive(ref remoteIpEndPoint); // Blocks until a message returns on this socket from a remote host.

        lock (incomingQueue)
        {
          incomingQueue.Enqueue(receiveBytes);
        }
      }
      catch (SocketException e)
      {
        // 10004 thrown when socket is closed
        if (e.ErrorCode != 10004) Debug.Log("Socket exception while receiving data from udp client: " + e.Message);
      }
      catch (Exception e)
      {
        Debug.Log("Error receiving data from udp client: " + e.Message);
      }
      Thread.Sleep(1);
    }
  }

  public byte[][] getMessages()
  {
    byte[][] pendingMessages;
    lock (incomingQueue)
    {
      pendingMessages = new byte[incomingQueue.Count][];
      int i = 0;
      while (incomingQueue.Count != 0)
      {
        pendingMessages[i] = incomingQueue.Dequeue();
        i++;
      }
    }

    return pendingMessages;
  }

  public void Send(byte[] bytes)
  {
    IPEndPoint serverEndpoint = new IPEndPoint(IPAddress.Parse(senderIp), senderPort);
    udpClient.Send(bytes, bytes.Length, serverEndpoint);
  }

  public void Stop()
  {
    threadRunning = false;
    receiveThread.Abort();
    udpClient.Close();
  }
}