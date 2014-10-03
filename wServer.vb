Option Strict Off
Option Explicit On
Imports System.IO
Imports System.Net
Imports System.Text
Imports System.Threading
Imports System.Net.Sockets

Public Class wServer
    Public Event StatusMessage(TheMessage As String)
    Private WebServer As New Thread(AddressOf WebServerThread)
    Private FilePath As String
    Private fileLength As Integer
    Private ServerPort As Integer

    Sub WebServerThread()
        Dim soc As New Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
        Try
            soc.Bind(New IPEndPoint(IPAddress.Any, ServerPort))
            soc.Listen(10)
            RaiseEvent StatusMessage("Stream Server Started: " & LocalIP & ":" & ServerPort)
            While True
                Dim ScallBack As AsyncCallback
                Dim HcallBack As AsyncCallback
                Dim client As Socket = soc.Accept
                Dim RecBuffer(client.ReceiveBufferSize) As Byte
                Dim PlayerData As String = ""
                client.Receive(RecBuffer)
                Dim ReceiveData As String = Encoding.ASCII.GetString(RecBuffer)
                RaiseEvent StatusMessage(ReceiveData)

                If InStr(ReceiveData, "video.mp4") <> 0 Then
                    Dim StartPosition As Long = 0

                    If InStr(ReceiveData, "Range: bytes=") <> 0 Then
                        Dim Spl1() As String, Spl2() As String
                        Spl1 = Split(ReceiveData, "Range: bytes=")
                        Spl2 = Split(Spl1(1), "-")
                        StartPosition = Spl2(0)
                    End If

                    If Len(FilePath) = 0 Then GoTo NoFile
                    If Len(Dir(FilePath, FileAttribute.Normal)) = 0 Then
                        RaiseEvent StatusMessage(FilePath & " Not Found")
                        GoTo NoFile
                    End If

                    fileLength = FileSystem.FileLen(FilePath)
                    RaiseEvent StatusMessage("Streaming: " & FilePath & " - " & client.RemoteEndPoint.ToString)
                    Dim StreamHeader As String = "HTTP/1.1 206 Partial Content" & vbCrLf & _
                                               "Server: DeskCast" & vbCrLf & _
                                               "Accept-Ranges: bytes" & vbCrLf & _
                                               "Content-Length: " & (fileLength - StartPosition) & vbCrLf & _
                                               "Content-Range: bytes " & StartPosition & "-" & (fileLength - 1) & "/" & fileLength & vbCrLf & _
                                               "Date: " & Date.Now.ToString("R") & vbCrLf & _
                                               "Connection: Close" & vbCrLf & _
                                               "Content-Type: video/mp4" & vbCrLf & vbCrLf

                    client.Send(Encoding.ASCII.GetBytes(StreamHeader), Encoding.ASCII.GetBytes(StreamHeader).Length, SocketFlags.None)
                    ScallBack = AddressOf FileSent
                    Dim bytesRead As Integer = Integer.MaxValue
                    Dim buffer(fileLength - StartPosition) As Byte
                    Dim inFile As New System.IO.FileStream(FilePath, IO.FileMode.Open, IO.FileAccess.Read)
                    inFile.Seek(StartPosition, IO.SeekOrigin.Begin)
                    bytesRead = inFile.Read(buffer, 0, buffer.Length)
                    Dim TmpFile As String = Application.UserAppDataPath & "\" & MD5Hash(GetUnixTimestamp()) & ".tmp"
                    Dim outFile As New System.IO.FileStream(TmpFile, IO.FileMode.Create, IO.FileAccess.Write)
                    outFile.Write(buffer, 0, bytesRead)
                    Thread.Sleep(100)
                    outFile.Close()
                    inFile.Close()
                    client.BeginSendFile(TmpFile, ScallBack, vbNull)
                    buffer = Nothing

                Else

                    If Mid(ReceiveData, 1, 4) = "GET " Or Mid(ReceiveData, 1, 5) = "POST " Then
                        Dim Spl1() As String, Spl2() As String, FPath As String, FLength As Long, FType As String = ""
                        ReceiveData = Replace(ReceiveData, "POST ", "GET ")
                        Spl1 = Split(ReceiveData, "GET ")
                        Spl2 = Split(Spl1(1), " HTTP")
                        Spl1 = Split(Spl2(0), "/")
                        Dim StrReqFile As String = Spl1(UBound(Spl1))
                        If Len(StrReqFile) = 0 Then GoTo NoFile

                        FPath = My.Application.Info.DirectoryPath & "\data\" & StrReqFile

                        If Len(FPath) = 0 Then GoTo NoFile
                        If Len(Dir(FPath, FileAttribute.Normal)) = 0 Then
                            RaiseEvent StatusMessage(FPath & " Not Found")
                            GoTo NoFile
                        End If

                        Spl1 = Split(StrReqFile, ".")
                        Select Case Spl1(1)
                            Case Is = "js"
                                FType = "application/javascript"
                            Case Is = "html"
                                FType = "text/html"
                            Case Is = "css"
                                FType = "text/css"
                            Case Is = "png"
                                FType = "image/png"
                            Case Is = "gif"
                                FType = "image/gif"
                        End Select


                        If StrReqFile = "player.js" Then
                            PlayerData = GetFile(FPath)
                            PlayerData = Replace(PlayerData, "$RECEIVER$NAME$", CC_Receiver)
                            FLength = Len(PlayerData)
                        Else
                            FLength = FileSystem.FileLen(FPath)
                        End If


                        Dim htmlHeader As String = "HTTP/1.1 200 OK" & vbCrLf & _
                                                 "Server: DeskCast" & vbCrLf & _
                                                 "Date: " & Date.Now.ToString("R") & vbCrLf & _
                                                 "Connection: close" & vbCrLf & _
                                                "Content-Length: " & FLength & vbCrLf & _
                                                "Content-Type: " & FType & vbCrLf & vbCrLf

                        If StrReqFile = "player.js" Then
                            client.Send(Encoding.ASCII.GetBytes(htmlHeader & PlayerData), Encoding.ASCII.GetBytes(htmlHeader & PlayerData).Length, SocketFlags.None)
                        Else
                            client.Send(Encoding.ASCII.GetBytes(htmlHeader), Encoding.ASCII.GetBytes(htmlHeader).Length, SocketFlags.None)
                            HcallBack = AddressOf HtmlSent
                            client.BeginSendFile(FPath, HcallBack, vbNull)
                        End If

                        GoTo FoundFile
                    End If

NoFile:

                    Dim htmlBody As String = "<html><head><title>HTTP 404 Not Found</title></head><body><table width='400' cellpadding='3' cellspacing='5'><tr><td id='tableProps' valign='top' align='left'></td><td id='tableProps2' align='left' valign='middle' width='360'><h1 id='errortype' style='COLOR: black; FONT: 13pt/15pt verdana'><span id='errorText'>The page cannot be found</span></h1> </td></tr><tr><td id='tablePropsWidth' width='400' colspan='2'><font style='COLOR: black; FONT: 8pt/11pt verdana'>The page you are looking for might have been removed, had its name changed, or is temporarily unavailable.</font></td></tr></table></body></html>"
                    Dim ErrorHeader As String = "HTTP/1.1 404 Not Found" & vbCrLf & _
                           "Server: DeskCast" & vbCrLf & _
                           "Date: " & Date.Now.ToString("R") & vbCrLf & _
                           "Connection: Close" & vbCrLf & _
                           "Content-Length: " & Len(htmlBody) & vbCrLf & _
                           "Content-Type: text/html" & vbCrLf & vbCrLf & htmlBody
                    RaiseEvent StatusMessage("404 Not Found - " & client.RemoteEndPoint.ToString)
                    client.Send(Encoding.ASCII.GetBytes(ErrorHeader), Encoding.ASCII.GetBytes(ErrorHeader).Length, SocketFlags.None)


FoundFile:
                End If

            End While
        Catch ex As Exception
            RaiseEvent StatusMessage("ERROR: " & ex.Message)
        End Try
    End Sub

    Public Sub StartServer(ListenPort As Integer)
        If WebServer.ThreadState = 8 Or WebServer.ThreadState = 16 Then
            ServerPort = ListenPort
            WebServer.Start()
        End If
    End Sub

    Public Sub VideoFile(StrFilePath As String)
        FilePath = StrFilePath
    End Sub

    Sub FileSent()
        CleanTempFiles()
        RaiseEvent StatusMessage("Stream Finished")
    End Sub

    Sub HtmlSent()
        RaiseEvent StatusMessage("HTML Object Sent")
    End Sub

    Public Sub CleanTempFiles()
        For Each filename As String In IO.Directory.GetFiles(Application.UserAppDataPath, "*.tmp")
            If FileInUse(filename) = False Then
                IO.File.Delete(filename)
            End If
        Next
    End Sub

    Protected Overrides Sub Finalize()
        CleanTempFiles()
        MyBase.Finalize()
    End Sub
End Class
