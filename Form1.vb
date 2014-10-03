Option Strict Off
Option Explicit On
Imports System.IO
Imports System.Net
Imports System.Threading
Partial Friend Class Form1
    Inherits System.Windows.Forms.Form
    Private tProcess As New Process
    Private WithEvents WC1 As New wClient
    Private WithEvents WS1 As New wSocket
    Private WithEvents WebSvr1 As New WServer
    Private WithEvents DSC1 As New cDiscover
    Private PauseTimeTrack As Boolean
    Private PauseVolTrack As Boolean
    Delegate Sub SetTextCallback([text] As String)
    Delegate Sub SetComboCallback([cItem] As String)
    Delegate Sub SetTrackTimeCallback([tMax] As Integer, [tValue] As Integer)
    Delegate Sub SetTrackVolumeCallback([tLevel] As Integer)
    Delegate Sub SetStateCallback([cState] As Integer)

    Private Sub SetState(ByVal [cState] As Integer)
        If Me.TxtState.InvokeRequired Then
            Dim d As New SetStateCallback(AddressOf SetState)
            Me.Invoke(d, New Object() {[cState]})
        Else
            Me.TxtState.Text = [cState]
            If [cState] = 1 Then
                Me.Timer2.Start()
            Else
                Me.Timer2.Stop()
            End If
        End If
    End Sub

    Private Sub SetText(ByVal [text] As String)
        If Me.TextBox1.InvokeRequired Then
            Dim d As New SetTextCallback(AddressOf SetText)
            Me.Invoke(d, New Object() {[text]})
        Else
            If CheckBox1.Checked = True Then
                Me.TextBox1.Text = Me.TextBox1.Text & [text] & vbCrLf
            End If
            End If
    End Sub

    Private Sub AddItem(ByVal [cItem] As String)
        If Me.ComboBox1.InvokeRequired Then
            Dim d As New SetComboCallback(AddressOf AddItem)
            Me.Invoke(d, New Object() {[cItem]})
        Else
            Dim X As Integer
            If ComboBox1.Items.Count > 0 Then
                For X = 0 To ComboBox1.Items.Count - 1
                    If ComboBox1.Items.Item(X) = [cItem] Then
                        GoTo IsListed
                    End If
                Next
            Else
            End If
            Me.ComboBox1.Items.Add([cItem])
IsListed:
            If Len(Me.ComboBox1.Text) = 0 Then Me.ComboBox1.Text = ComboBox1.Items.Item(0)
            End If
    End Sub

    Private Sub TrackTime([tMax] As Integer, [tValue] As Integer)
        If Me.TrackBar1.InvokeRequired Then
            Dim d As New SetTrackTimeCallback(AddressOf TrackTime)
            Me.Invoke(d, New Object() {[tMax], [tValue]})
        Else
            If PauseTimeTrack = False Then
                If IsTranscoding = True Then
                    [tMax] = TransCodeDuration
                End If
                If [tMax] < [tValue] Then [tMax] = [tValue]
                If [tMax] > 0 Then Me.TrackBar1.Maximum = [tMax]
                If [tValue] > 0 Then Me.TrackBar1.Value = [tValue]
            End If
            End If
    End Sub

    Private Sub TrackVolume([tLevel] As Integer)
        If Me.TrackBar2.InvokeRequired Then
            Dim d As New SetTrackVolumeCallback(AddressOf TrackVolume)
            Me.Invoke(d, New Object() {[tLevel]})
        Else
            If PauseVolTrack = False Then
                Me.TrackBar2.Value = [tLevel]
            End If
            End If
    End Sub

    Private Sub BackgroundWorker1_DoWork(ByVal sender As System.Object, ByVal e As System.ComponentModel.DoWorkEventArgs) Handles BackgroundWorker1.DoWork
        StartTransCodeServer()
    End Sub

    Public Sub LoadLocalStream(LocalFile As String)
        If InStr(LocalFile, ".mp4") <> 0 Or InStr(LocalFile, ".avi") <> 0 Or InStr(LocalFile, ".mkv") <> 0 Or InStr(LocalFile, ".flv") <> 0 Or InStr(LocalFile, ".mpg") <> 0 Or InStr(LocalFile, ".webm") <> 0 Or InStr(LocalFile, ".divx") <> 0 Or InStr(LocalFile, ".mov") <> 0 Or InStr(LocalFile, ".m4v") <> 0 Or InStr(LocalFile, ".wmv") <> 0 Then
            If WS1.IsSocketOpen = False Then
                IsLocalStream = True
                TempFilePath = LocalFile
                ConnectChromecast()
                Timer4.Start()
                Exit Sub
            End If
            WebSvr1.CleanTempFiles()
            If IsTranscoding = True Then
                If BackgroundWorker1.IsBusy = True Then BackgroundWorker1.CancelAsync()
                If tProcess.HasExited = False Then tProcess.Kill()
            End If
            VideoTitle = LocalFile
            VideoTitle = Split(LocalFile, "\")(UBound(Split(LocalFile, "\")))
            Label3.Text = VideoTitle
            If InStr(LocalFile, ".mp4") <> 0 Then
                IsTranscoding = False
                SetText("Streaming: " & LocalFile)
                WebSvr1.VideoFile(LocalFile)
                WS1.SendMessage(LoadPlayURL(VideoTitle, "http://" & LocalIP & ":" & LocalPort & "/video.mp4"))
            Else
                Thread.Sleep(1000)
                SetText("Transcoding: " & LocalFile)
                TrancodeFilePath = LocalFile
                If IsTranscoding = True And BackgroundWorker1.IsBusy = True Then
                    If BackgroundWorker1.CancellationPending = True Then
                        Timer3.Start()
                    End If
                    Exit Sub
                End If
                IsTranscoding = True
                BackgroundWorker1.WorkerSupportsCancellation = True
                BackgroundWorker1.RunWorkerAsync()
            End If
        End If
    End Sub


    Public Sub LoadNetWorkStream(Networkfile As String)
        If InStr(Networkfile, ".mp4") <> 0 Or InStr(Networkfile, ".avi") <> 0 Or InStr(Networkfile, ".mkv") <> 0 Or InStr(Networkfile, ".flv") <> 0 Or InStr(Networkfile, ".mpg") <> 0 Or InStr(Networkfile, ".webm") <> 0 Or InStr(Networkfile, ".divx") <> 0 Or InStr(Networkfile, ".mov") <> 0 Or InStr(Networkfile, ".m4v") <> 0 Or InStr(Networkfile, ".wmv") <> 0 Then
            If WS1.IsSocketOpen = False Then
                IsLocalStream = False
                TempFilePath = Networkfile
                ConnectChromecast()
                Timer4.Start()
                Exit Sub
            End If
            WebSvr1.CleanTempFiles()
            If IsTranscoding = True Then
                If BackgroundWorker1.IsBusy = True Then BackgroundWorker1.CancelAsync()
                If tProcess.HasExited = False Then tProcess.Kill()
            End If
            Dim VideoURL As String = Networkfile
            Dim Videofile As String = Split(VideoURL, "/")(UBound(Split(VideoURL, "/")))
            Label3.Text = Videofile
            If Mid(LCase(Videofile), Len(Videofile) - 3, 4) = ".mp4" Then
                IsTranscoding = False
                VideoTitle = Videofile
                WS1.SendMessage(LoadPlayURL(VideoTitle, VideoURL))
            Else
                Thread.Sleep(1000)
                TrancodeFilePath = VideoURL
                VideoTitle = Videofile
                SetText("Transcoding: " & VideoURL)
                If IsTranscoding = True And BackgroundWorker1.IsBusy = True Then
                    If BackgroundWorker1.CancellationPending = True Then
                        Timer3.Start()
                    End If
                    Exit Sub
                End If
                IsTranscoding = True
                BackgroundWorker1.WorkerSupportsCancellation = True
                BackgroundWorker1.RunWorkerAsync()
            End If
        End If
    End Sub

    Private Sub StartTransCodeServer()
        Control.CheckForIllegalCrossThreadCalls = False
        Dim startinfo As New System.Diagnostics.ProcessStartInfo
        Dim sReader As StreamReader
        Dim tOutput As String
        startinfo.FileName = Transcoder
        startinfo.Arguments = " -i " & Chr(34) & TrancodeFilePath & Chr(34) & " -b:v 3000k -s 1280x720 -ar 44100 -ac 2 -f matroska tcp://" & LocalIP & ":" & TranscodePort & "?listen"
        startinfo.UseShellExecute = False
        startinfo.WindowStyle = ProcessWindowStyle.Hidden
        startinfo.RedirectStandardError = True
        startinfo.RedirectStandardOutput = True
        startinfo.CreateNoWindow = True
        tProcess.StartInfo = startinfo
        tProcess.Start()
        SetText("Transcode Server Enabled")
        sReader = tProcess.StandardError
        Do
            If BackgroundWorker1.CancellationPending Then Exit Sub
            tOutput = sReader.ReadLine
            If InStr(tOutput, "Duration: ") <> 0 Then
                Dim Spl1() As String, Spl2() As String
                Spl1 = Split(tOutput, "Duration: ")
                Spl2 = Split(Spl1(1), ",")
                TransCodeDuration = TimeSpan.Parse(Trim(Spl2(0))).TotalSeconds
                Thread.Sleep(300)
                WS1.SendMessage(LoadPlayURL(VideoTitle, "http://" & LocalIP & ":" & TranscodePort))
            End If
            'SetText(tOutput)
        Loop Until tProcess.HasExited And tOutput = Nothing Or tOutput = ""
        SetText("Transcode Server Disabled")
    End Sub

    Private Sub CC1_GotChannel(ByRef PlData As String) Handles WC1.GotChannel
        Dim Spl1() As String, Spl2() As String
        If InStr(PlData, Chr(34) & "URL" & Chr(34) & ":" & Chr(34) & "ws") <> 0 Then
            Timer1.Enabled = False
            SetText(PlData)
            Spl1 = Split(PlData, Chr(34) & "URL" & Chr(34) & ":" & Chr(34))
            Spl2 = Split(Spl1(1), Chr(34))
            CmdID = 0
            WS1.ConnectWs(Spl2(0))
        End If
    End Sub

    Private Sub CC1_GotInit(ByRef PlData As String) Handles WC1.GotInit
        SetText(PlData)
        Timer1.Start()
    End Sub

    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
        WC1.PostChannel()
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        ConnectChromecast()
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        DisConnectChromecast()
    End Sub

    Public Sub ConnectChromecast()
        If Len(ComboBox1.Text) = 0 Then Exit Sub
        If WS1.IsSocketOpen = False Then
            ChromeCastIP = Split(ComboBox1.Text, " / ")(1)
            ChromeCastUID = Split(ComboBox1.Text, " / ")(2)
            WC1.PostInit()
        End If
    End Sub

    Public Sub DisConnectChromecast()
        Timer1.Stop()
        WC1.DelReceiver()
    End Sub

    Private Sub WS1_WsClosed() Handles WS1.WsClosed
        SetText("Closed")
        Label3.Text = ""
        SetState(0)
    End Sub

    Private Sub WS1_WsConnected() Handles WS1.WsConnected
        SetText("Connected")
    End Sub

    Private Sub WS1_WsError() Handles WS1.WsError
        SetText("Error")
        Label3.Text = ""
    End Sub

    Private Sub WS1_WsMessageReceived(StrMessage As String) Handles WS1.WsMessageReceived
        Control.CheckForIllegalCrossThreadCalls = False
        SetText(StrMessage)
        If InStr(StrMessage, Chr(34) & "type" & Chr(34) & ":" & Chr(34) & "STATUS" & Chr(34)) <> 0 Or InStr(StrMessage, Chr(34) & "type" & Chr(34) & ":" & Chr(34) & "RESPONSE" & Chr(34)) <> 0 Then
            Dim Spl1() As String, Spl2() As String
            Dim pTime As Integer
            Dim pDuration As Integer


            If InStr(StrMessage, Chr(34) & "state" & Chr(34) & ":") <> 0 Then
                Spl1 = Split(StrMessage, Chr(34) & "state" & Chr(34) & ":")
                Dim pState As Integer = Mid(Spl1(1), 1, 1)
                If pState = 2 Then
                    SetState(1)
                Else
                    SetState(0)
                End If
            End If

            If InStr(StrMessage, Chr(34) & "duration" & Chr(34) & ":") <> 0 Then
                Spl1 = Split(StrMessage, Chr(34) & "duration" & Chr(34) & ":")
                Spl2 = Split(Spl1(1), ",")
                If Spl2(0) = "null" Then Spl2(0) = 0
                pDuration = Fix(Spl2(0))
            End If

            If InStr(StrMessage, Chr(34) & "current_time" & Chr(34) & ":") <> 0 Then
                Spl1 = Split(StrMessage, Chr(34) & "current_time" & Chr(34) & ":")
                Spl2 = Split(Spl1(1), ",")
                If Spl2(0) = "null" Then Spl2(0) = 0
                pTime = Fix(Spl2(0))
                TrackTime(pDuration, pTime)
            End If

            If InStr(StrMessage, Chr(34) & "volume" & Chr(34) & ":") <> 0 Then
                Spl1 = Split(StrMessage, Chr(34) & "volume" & Chr(34) & ":")
                Spl2 = Split(Spl1(1), ",")
                If Spl2(0) = "null" Then Spl2(0) = 0
                If Len(Spl2(0)) >= 3 Then
                    Spl2(0) = Mid(Spl2(0), 3, 1)
                Else
                    If Spl2(0) = "1" Then Spl2(0) = "10"
                End If
                Dim pVolume As Integer = Spl2(0)
                TrackVolume(pVolume)
            End If

            If InStr(StrMessage, Chr(34) & "muted" & Chr(34) & ":") <> 0 Then
                Spl1 = Split(StrMessage, Chr(34) & "muted" & Chr(34) & ":")
                Spl2 = Split(Spl1(1), ",")
                Dim pMuteState As Boolean = Spl2(0)
                If pMuteState = False Then
                    Button5.Text = "Mute"
                Else
                    Button5.Text = "Unmute"
                End If
            End If

        End If
    End Sub

    Private Sub TextBox1_TextChanged(sender As Object, e As EventArgs) Handles TextBox1.TextChanged
        If Len(TextBox1.Text) = 0 Then Exit Sub
        TextBox1.SelectionStart = Len(TextBox1.Text)
        TextBox1.ScrollToCaret()
    End Sub

    Private Sub Form1_DragDrop(sender As System.Object, e As System.Windows.Forms.DragEventArgs) Handles Me.DragDrop
        Dim theFiles() As String = CType(e.Data.GetData("FileDrop", True), String())
        For Each theFile As String In theFiles
            LoadLocalStream(theFile)
        Next
    End Sub

    Private Sub Form1_DragEnter(sender As System.Object, e As System.Windows.Forms.DragEventArgs) Handles Me.DragEnter
        If e.Data.GetDataPresent(DataFormats.FileDrop) Then
            e.Effect = DragDropEffects.Copy
        End If
    End Sub

    Private Sub Form1_FormClosed(sender As Object, e As FormClosedEventArgs) Handles Me.FormClosed
        End
    End Sub

    Private Sub Form1_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        Try
            SaveSetting("DeskCast", "Options", "ROOTED", IsRooted)
            DSC1.StopDiscovery()
            WebSvr1.CleanTempFiles()
            If BackgroundWorker1.IsBusy = True Then BackgroundWorker1.CancelAsync()
            tProcess.Kill()
            End
        Catch ex As Exception
            End
        End Try
    End Sub

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.Show()
        IsRooted = GetSetting("DeskCast", "Options", "ROOTED", "0")
        If IsRooted = "0" Then
            RadioButton1.Checked = True
            RadioButton2.Checked = False
        Else
            RadioButton2.Checked = True
            RadioButton1.Checked = False
        End If
        LocalIP = DSC1.GetLocalIP
        Dim RPort As System.Random = New System.Random()
        LocalPort = RPort.Next(9000, 9500)
        TranscodePort = RPort.Next(9600, 10100)
        SetText("Local IP: " & LocalIP)
        WebSvr1.StartServer(LocalPort)
        DSC1.DiscoverChromeCast()
    End Sub

    Private Sub Button4_Click(sender As Object, e As EventArgs) Handles Button4.Click
        WS1.SendMessage(Pause)
    End Sub

    Private Sub Button5_Click(sender As Object, e As EventArgs) Handles Button5.Click
        If Button5.Text = "Mute" Then
            WS1.SendMessage(Mute)
        Else
            WS1.SendMessage(UnMute)
        End If
    End Sub

    Private Sub WebSvr1_StatusMessage(TheMessage As String) Handles WebSvr1.StatusMessage
        SetText(TheMessage)
    End Sub

    Private Sub DSCe1_DiscoveredCC(ChromeCast_Name As String, ChromeCast_IP As String, ChromeCast_BaseURL As String, ChromeCast_UID As String) Handles DSC1.DiscoveredCC
        SetText("Found: " & ChromeCast_Name & " / " & ChromeCast_IP & " / " & ChromeCast_UID)
        AddItem(ChromeCast_Name & " / " & ChromeCast_IP & " / " & ChromeCast_UID)
    End Sub

    Private Sub Button10_Click(sender As Object, e As EventArgs) Handles Button10.Click
        ComboBox1.Items.Clear()
        ComboBox1.Text = Nothing
        DSC1.DiscoverChromeCast()
    End Sub

    Private Sub TrackBar1_MouseDown(sender As Object, e As MouseEventArgs) Handles TrackBar1.MouseDown
        PauseTimeTrack = True
    End Sub

    Private Sub TrackBar1_MouseUp(sender As Object, e As MouseEventArgs) Handles TrackBar1.MouseUp
        WS1.SendMessage(ResumePlay(ResumePosition))
        PauseTimeTrack = False
    End Sub

    Private Sub TrackBar1_Scroll(sender As Object, e As EventArgs) Handles TrackBar1.Scroll
        Label1.Text = TimeSpan.FromSeconds(TrackBar1.Value).ToString
    End Sub

    Private Sub TrackBar1_ValueChanged(sender As Object, e As EventArgs) Handles TrackBar1.ValueChanged
            Label1.Text = TimeSpan.FromSeconds(TrackBar1.Value).ToString
            Label2.Text = TimeSpan.FromSeconds(TrackBar1.Maximum).ToString
            ResumePosition = TrackBar1.Value
    End Sub

    Private Sub Timer2_Tick(sender As Object, e As EventArgs) Handles Timer2.Tick
        If WS1.IsSocketOpen = False Then SetState(0)
        WS1.SendMessage(Status)
    End Sub

    Private Sub TrackBar2_MouseDown(sender As Object, e As MouseEventArgs) Handles TrackBar2.MouseDown
        PauseVolTrack = True
    End Sub

    Private Sub TrackBar2_MouseUp(sender As Object, e As MouseEventArgs) Handles TrackBar2.MouseUp
        If TrackBar2.Value = 10 Then
            WS1.SendMessage(Volume("1"))
        Else
            WS1.SendMessage(Volume("0." & TrackBar2.Value))
        End If
        PauseVolTrack = False
    End Sub

    Private Sub Button8_Click(sender As Object, e As EventArgs) Handles Button8.Click
        WS1.SendMessage(ResumePlay(ResumePosition))
    End Sub

    Private Sub Timer3_Tick(sender As Object, e As EventArgs) Handles Timer3.Tick
        If BackgroundWorker1.CancellationPending = False Then
            Timer3.Stop()
            BackgroundWorker1.WorkerSupportsCancellation = True
            BackgroundWorker1.RunWorkerAsync()
        End If
    End Sub

    Private Sub LoadFileFromURLToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles LoadFileFromURLToolStripMenuItem.Click
        Dim StrDef As String = Clipboard.GetText()
        If Not Mid(StrDef, 1, 4) = "http" Then StrDef = ""
        Dim Result As String = LCase(InputBox("Input a URL to a video file" & vbCrLf & vbCrLf & "Supported Formats: mp4, avi, mkv, flv, mpg, webm, divx, mov, m4v, wmv", "Load File From The Web", StrDef, Me.Left, Me.Top))
        LoadNetWorkStream(Result)
    End Sub

    Private Sub SelectFileFromComputerToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles SelectFileFromComputerToolStripMenuItem.Click
        OpenFileDialog1.FileName = ""
        OpenFileDialog1.Filter = "Video Files|*.*"
        OpenFileDialog1.Multiselect = False
        OpenFileDialog1.Title = "Select Video File To Cast"
        OpenFileDialog1.ShowDialog()
        LoadLocalStream(OpenFileDialog1.FileName)
    End Sub

    Private Sub RadioButton1_Click(sender As Object, e As EventArgs) Handles RadioButton1.Click
        IsRooted = "0"
    End Sub

    Private Sub RadioButton2_Click(sender As Object, e As EventArgs) Handles RadioButton2.Click
        IsRooted = "1"
    End Sub

    Private Sub Timer4_Tick(sender As Object, e As EventArgs) Handles Timer4.Tick
        Timer4.Stop()
        If IsLocalStream = True Then
            LoadLocalStream(TempFilePath)
        Else
            LoadNetWorkStream(TempFilePath)
        End If
    End Sub
End Class