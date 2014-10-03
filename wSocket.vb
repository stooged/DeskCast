Option Explicit On
Imports WebSocket4Net.WebSocket
Public Class wSocket
    Public Event WsConnected()
    Public Event WsClosed()
    Public Event WsError()
    Public Event WsMessageReceived(StrMessage As String)
    Public Event WsDataReceived(StrData As String)
    Dim wSock As WebSocket4Net.WebSocket ' uses WebSocket4Net.dll
    'https:// websocket4net.codeplex.com/


    Public Sub ConnectWs(TheWsUrl As String, Optional TheProtocol As String = "")
        Dim wSckt As New WebSocket4Net.WebSocket(TheWsUrl, TheProtocol, WebSocket4Net.WebSocketVersion.Rfc6455)
        wSock = wSckt
        wsock.EnableAutoSendPing = False
        AddHandler wSock.Opened, Sub(s, e) socketOpened()
        AddHandler wSock.Error, Sub(s, e) socketError()
        AddHandler wSock.Closed, Sub(s, e) socketClosed()
        AddHandler wSock.MessageReceived, Sub(s, e) socketMessage(e)
        AddHandler wSock.DataReceived, Sub(s, e) socketDataReceived(e)
        wSock.Open()
    End Sub

    Public Sub CloseWs()
        wsock.Close()
        wsock = Nothing
    End Sub

    Public Sub SendMessage(TheMessage As String)
        If IsSocketOpen() = True Then
            wSock.Send(TheMessage)
        End If
    End Sub

    Public Function IsSocketOpen() As Boolean
        If wSock Is Nothing Then IsSocketOpen = False : Exit Function
        If wSock.State = WebSocket4Net.WebSocketState.Open Then
            IsSocketOpen = True
        Else
            IsSocketOpen = False
        End If
    End Function

    Sub socketOpened()
        RaiseEvent WsConnected()
    End Sub
    Sub socketClosed()
        RaiseEvent WsClosed()
    End Sub
    Sub socketError()
        RaiseEvent WsError()
    End Sub
    Sub socketMessage(e As WebSocket4Net.MessageReceivedEventArgs)
        If e.Message = "[" & Chr(34) & "cm" & Chr(34) & ",{" & Chr(34) & "type" & Chr(34) & ":" & Chr(34) & "ping" & Chr(34) & "}]" Then
            SendMessage("[" & Chr(34) & "cm" & Chr(34) & ",{" & Chr(34) & "type" & Chr(34) & ":" & Chr(34) & "pong" & Chr(34) & "}]")
        Else
            RaiseEvent WsMessageReceived(e.Message)
        End If
    End Sub
    Sub socketDataReceived(e As WebSocket4Net.DataReceivedEventArgs)
        RaiseEvent WsDataReceived(System.Text.Encoding.Default.GetString(e.Data))
    End Sub
End Class
