using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Diagnostics;
using Newtonsoft.Json;
using UnityEngine;
using UnityEditor;

struct EditorMatch
{
    public string ID;
    public int Typ;
    public int Port;
}

public class AppEditor : EditorWindow {

    private string host = "127.0.0.1";
    private string port_account = "30001";
    private string port_match = "30002";
    private string port_characters = "30003";
    private string port_priv = "30004";

    private string serverPath;
    private EditorMatch[] matches;

    [MenuItem("AppEditor/Control Panel")]
    public static void ShowWindow()
    {
        GetWindow(typeof(AppEditor));
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Documentation", EditorStyles.boldLabel);
        if(GUILayout.Button("Open Web Documentation"))
        {
            Application.OpenURL("http://www.farkow.com");
        }

        GUILayout.Space(16f);
        

        EditorGUILayout.LabelField("Services", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Service Server Host:");
        host = EditorGUILayout.TextField(host);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Account Service:");
        port_account = EditorGUILayout.TextField(port_account);
        if (GUILayout.Button("Check \"Account\" Service"))
        {
            UDPTest(host, port_account);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Match Service:");
        port_match = EditorGUILayout.TextField(port_match);
        if (GUILayout.Button("Check \"Match\" Service"))
        {
            UDPTest(host, port_match);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Characters Service:");
        port_characters = EditorGUILayout.TextField(port_characters);
        if (GUILayout.Button("Check \"Character\" Service"))
        {
            UDPTest(host, port_characters);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Priv Service:");
        port_priv = EditorGUILayout.TextField(port_priv);
        if (GUILayout.Button("Check \"Priv\" Service"))
        {
            UDPTest(host, port_priv);
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(16f);


        EditorGUILayout.LabelField("Server", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Path to Server Executable:");
        serverPath = EditorGUILayout.TextField(serverPath);
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Load Available Matches"))
        {
            UDPMatches(host, port_priv);
        }

        if (matches != null && matches.Length > 0)
        {
            EditorGUILayout.LabelField("Available Matches", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("ID", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Type", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Port", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(" ");
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(6f);

            foreach(EditorMatch sm in matches)
            {
                // TODO show only play ones

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(sm.ID.ToString());
                EditorGUILayout.LabelField(((MatchType)sm.Typ).ToString());
                EditorGUILayout.LabelField(sm.Port.ToString());
                if(GUILayout.Button("Start"))
                {
                    // TODO do it with ID after server update
                    ServerStart(sm.ID); 
                }
                if(GUILayout.Button("Copy"))
                {
                    EditorGUIUtility.systemCopyBuffer = sm.ID.ToString();
                }
                EditorGUILayout.EndHorizontal();
            }

        }
    }

    private static UdpClient UDPClient(int port)
    {
        UdpClient client = new UdpClient();
        int portClient = 0;
        do
        {
            portClient = UnityEngine.Random.Range(10000, 60000);
            try
            {
                client = new UdpClient(portClient);
                client.Client.SendTimeout = 2000;
            }
            catch (Exception exc)
            {
                portClient = 0;
                UnityEngine.Debug.LogError(exc);
            }
        } while (port == 0);
        return client;
    }

    private void UDPTest(string host, string p)
    {
        int port = int.Parse(p);
        UdpClient client = UDPClient(port);
        client.Client.ReceiveTimeout = 2000;
        client.Client.SendTimeout = 2000;

        IPAddress[] ips = Dns.GetHostAddresses(host);
        int ip = 0;
        if(ips.Length == 0)
        {
            EditorUtility.DisplayDialog("Service Connectivity Test", 
                "Failed!", "OK");
            return;
        } else
        {
            for(int j = 0; j < ips.Length; j++)
            {
                UnityEngine.Debug.Log(
                    "IP address found: " + ips[j].ToString());
                ip = j;
            }
        }

        IPEndPoint service = new IPEndPoint(ips[ip], port);
        
        byte[] data = Encoding.UTF8.GetBytes("{\"ID\":\"\",\"Action\":0,"+
            "\"IP\":\"\",\"Request\":\"{}\",\"Response\":\"\",\"Error\":0}");
        client.Send(data, data.Length, service);

        IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
        string response = "";
        try
        {
            data = client.Receive(ref anyIP);
            response = Encoding.UTF8.GetString(data);
        }
        catch{}

        EditorUtility.DisplayDialog("Service Connectivity Test", 
            (response.Length > 0 && 
            response.Substring(0, 1) == "{") ? "Success!" : "Failed!", "OK");

        client.Close();
    }

    private void UDPMatches(string host, string p)
    {
        int port = int.Parse(p);
        UdpClient client = UDPClient(port);
        client.Client.ReceiveTimeout = 2000;
        client.Client.SendTimeout = 2000;

        IPAddress ip = IPAddress.Parse(host);
        IPEndPoint service = new IPEndPoint(ip, port);

        byte[] data = Encoding.UTF8.GetBytes("{\"ID\":\"\",\"Action\":" + 
            ((int)UDPAction.Matches).ToString() + "," +
            "\"IP\":\"\",\"Request\":\"{}\",\"Response\":\"\",\"Error\":0}");
        client.Send(data, data.Length, service);

        IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
        string response = "";
        try
        {
            data = client.Receive(ref anyIP);
            response = Encoding.UTF8.GetString(data);

            UDPPacket packet = JsonConvert.DeserializeObject<UDPPacket>(response);
            matches = JsonConvert.DeserializeObject<EditorMatch[]>(packet.Response);
        }
        catch(Exception e) {
            EditorUtility.DisplayDialog("Server Communication Failed",
                e.Message, "OK");
        }
        client.Close();
    }

    private void ServerStart(string id)
    {
        try
        {
            Process p = new Process();
            p.StartInfo.FileName = serverPath;
            p.StartInfo.Arguments = " -id " + id;
            p.Start();
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError("Cannot start the server!: " + 
                ex.Message);
        }
    }

    private void ClientStart(string email)
    {
        /*
        try
        {
            Process p = new Process();
            p.StartInfo.FileName = clientPath;
            p.StartInfo.Arguments = " -host " + host +
                " -port " + port.ToString();
            p.Start();
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError("Cannot start the server!: " +
                ex.Message);
        }
        */
    }
}
