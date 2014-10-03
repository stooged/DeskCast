Option Strict Off
Option Explicit On
Imports System.Net
Imports System.Text
Imports System.Threading
Imports System.Net.Sockets

Public Class cDiscover
    Public Event DiscoveredCC(ChromeCast_Name As String, ChromeCast_IP As String, ChromeCast_BaseURL As String, ChromeCast_UID As String)
    Public Event DiscoverError(ErrorMessage As String)
    Private listenPort As Integer
    Private SsDPThreads As Thread
    Private Dlisteners As UdpClient
    Private CancelListen As Boolean = False

    Private Function RandomPort() As Integer
        Dim RndPort As System.Random = New System.Random()
        Return RndPort.Next(50000, 58000)
    End Function

    Private Sub SSDPReceive()
        Dim Dlistener As New UdpClient(listenPort)
        Dim DEP As New IPEndPoint(IPAddress.Any, listenPort)
        Dlisteners = Dlistener
        Dim sBytes As [Byte]() = System.Text.Encoding.ASCII.GetBytes("M-SEARCH * HTTP/1.1" & vbCrLf & _
                                              "HOST: 239.255.255.250:1900" & vbCrLf & _
                                              "MAN: " & Chr(34) & "ssdp:discover" & Chr(34) & vbCrLf & _
                                              "MX: 5" & vbCrLf & _
                                              "ST: urn:dial-multiscreen-org:service:dial:1" & vbCrLf & vbCrLf)
        Dlistener.Send(sBytes, sBytes.Length, "239.255.255.250", 1900)
        Try
            While Not CancelListen
                Dim lBytes As Byte() = Dlistener.Receive(DEP)
                Dim Result As String = Encoding.ASCII.GetString(lBytes, 0, lBytes.Length)
                Dim Spl1() As String, Spl2() As String
                If InStr(Result, "LOCATION: ") <> 0 Then
                    Spl1 = Split(Result, "LOCATION: ")
                    Spl2 = Split(Spl1(1), vbCrLf)
                    GetCCInfo(Spl2(0), DEP.Address.ToString)
                End If
            End While
        Catch e As Exception
            RaiseEvent DiscoverError(e.ToString())
        Finally
            Dlistener.Close()
        End Try
    End Sub

    Private Sub GetCCInfo(StrLocation As String, StrIP As String)
        Try
            Dim oWeb As New System.Net.WebClient()
            Dim Result As String = oWeb.DownloadString(StrLocation)
            Dim CC_Base As String = ""
            If InStr(Result, "<friendlyName>") <> 0 Then
                Dim Spl1() As String
                Dim Spl2() As String
                Spl1 = Split(Result, "<friendlyName>")
                Spl2 = Split(Spl1(1), "</friendlyName>")
                Dim CC_Name As String = Spl2(0)
                Spl1 = Split(Result, "<modelName>")
                Spl2 = Split(Spl1(1), "</modelName>")
                Dim CC_Model As String = Spl2(0)
                If Not CC_Model = "Eureka Dongle" Then GoTo NotCC
                If InStr(Result, "<URLBase>") <> 0 Then
                    Spl1 = Split(Result, "<URLBase>")
                    Spl2 = Split(Spl1(1), "</URLBase>")
                    CC_Base = Spl2(0)
                End If
                Spl1 = Split(Result, "<UDN>")
                Spl2 = Split(Spl1(1), "</UDN>")
                Dim CC_UID As String = Replace(Spl2(0), "uuid:", "")
                RaiseEvent DiscoveredCC(CC_Name, StrIP, CC_Base, CC_UID)
NotCC:
            End If
        Catch ex As Exception
            RaiseEvent DiscoverError(ex.Message)
        End Try
    End Sub

    Public Function GetLocalIP() As String
        Dim host As System.Net.IPHostEntry = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName)
        Dim strRet As String = Nothing
        For Each ip As System.Net.IPAddress In host.AddressList
            If ip.AddressFamily = Net.Sockets.AddressFamily.InterNetwork Then
                strRet = ip.ToString
                Exit For
            End If
        Next
        Return strRet
    End Function

    Public Sub DiscoverChromeCast()
        If SsDPThreads IsNot Nothing Then
            If SsDPThreads.IsAlive = True Then
                Dlisteners.Close()
                CancelListen = True
                Thread.Sleep(500)
                SsDPThreads.Abort()
            End If
        End If
        Dim SsDPThread As New Thread(AddressOf SSDPReceive)
        SsDPThreads = SsDPThread
        CancelListen = False
        SsDPThread.Start()
    End Sub

    Sub StopDiscovery()
        If SsDPThreads IsNot Nothing Then
            If SsDPThreads.IsAlive = True Then
                Dlisteners.Close()
                CancelListen = True
                Thread.Sleep(10)
                SsDPThreads.Abort()
            End If
        End If
    End Sub

    Protected Overrides Sub Finalize()
        MyBase.Finalize()
        If SsDPThreads IsNot Nothing Then
            If SsDPThreads.IsAlive = True Then
                Dlisteners.Close()
                CancelListen = True
                Thread.Sleep(10)
                SsDPThreads.Abort()
            End If
        End If
    End Sub
End Class
