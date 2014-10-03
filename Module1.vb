Option Strict Off
Option Explicit On
Imports System.Text
Imports System.Security.Cryptography
Module Module1
    '##############private whitelist server sql entry################
    '
    '    INSERT INTO `pwl-custom_apps` (`ID`, `name`, `v2app`, `test_app`, `content`) VALUES(4, 'DeskCast', 0, 0, '{"use_channel":true,"allow_empty_post_data":true,"app_id":"DeskCast","url":"${POST_DATA}","dial_enabled":true}');
    '
    '################################################################

    Public Const Transcoder As String = "ffmpeg.dll" ' .exe renamed to .dll
    'http:// ffmpeg.zeranoe.com/builds/win32/static/ffmpeg-20140919-git-33c752b-win32-static.7z

    Public CmdID As Long, ChromeCastIP As String, LocalIP As String, LocalPort As Integer, TranscodePort As Integer, ChromeCastUID As String, TrancodeFilePath As String, ResumePosition As Integer, TransCodeDuration As Integer, IsTranscoding As Boolean, VideoTitle As String, IsRooted As String, TempFilePath As String, IsLocalStream As Boolean

    Public Function CC_Receiver() As String
        If IsRooted = "0" Then
            CC_Receiver = "18a8aeaa-8e3d-4c24-b05d-da68394a3476_1" ' should work with non rooted chromecasts(does not use local receiver files in data directory)
        Else
            CC_Receiver = "Fling" ' should work with rooted chromecasts and team eureka custom whitelist
            ' CC_Receiver = "DeskCast"   ' private whitelist server
        End If
    End Function

    Public Function LoadPlayURL(VideoTitle As String, VideoURL As String) As String
        LoadPlayURL = "[" & Chr(34) & "ramp" & Chr(34) & ",{" & Chr(34) & "title" & Chr(34) & ":" & Chr(34) & VideoTitle & Chr(34) & "," & Chr(34) & "src" & Chr(34) & ":" & Chr(34) & VideoURL & Chr(34) & "," & Chr(34) & "type" & Chr(34) & ":" & Chr(34) & "LOAD" & Chr(34) & "," & Chr(34) & "cmd_id" & Chr(34) & ":" & CmdID & "," & Chr(34) & "autoplay" & Chr(34) & ":true}]"
        CmdID = CmdID + 1
    End Function

    Public Function Status() As String
        Status = "[" & Chr(34) & "ramp" & Chr(34) & ",{" & Chr(34) & "type" & Chr(34) & ":" & Chr(34) & "INFO" & Chr(34) & "," & Chr(34) & "cmd_id" & Chr(34) & ":" & CmdID & "}]"
        CmdID = CmdID + 1
    End Function

    Public Function Pause() As String
        Pause = "[" & Chr(34) & "ramp" & Chr(34) & ",{" & Chr(34) & "type" & Chr(34) & ":" & Chr(34) & "STOP" & Chr(34) & "," & Chr(34) & "cmd_id" & Chr(34) & ":" & CmdID & "}]"
        CmdID = CmdID + 1
    End Function

    Public Function ResumePlay(VideoPosition As String) As String
        ResumePlay = "[" & Chr(34) & "ramp" & Chr(34) & ",{" & Chr(34) & "position" & Chr(34) & ":" & VideoPosition & "," & Chr(34) & "type" & Chr(34) & ":" & Chr(34) & "PLAY" & Chr(34) & "," & Chr(34) & "cmd_id" & Chr(34) & ":" & CmdID & "}]"
        CmdID = CmdID + 1
    End Function

    Public Function Play() As String
        Play = "[" & Chr(34) & "ramp" & Chr(34) & ",{" & Chr(34) & "type" & Chr(34) & ":" & Chr(34) & "PLAY" & Chr(34) & "," & Chr(34) & "cmd_id" & Chr(34) & ":" & CmdID & "}]"
        CmdID = CmdID + 1
    End Function

    Public Function Mute() As String
        Mute = "[" & Chr(34) & "ramp" & Chr(34) & ",{" & Chr(34) & "type" & Chr(34) & ":" & Chr(34) & "VOLUME" & Chr(34) & "," & Chr(34) & "cmd_id" & Chr(34) & ":" & CmdID & "," & Chr(34) & "muted" & Chr(34) & ":true}]"
        CmdID = CmdID + 1
    End Function

    Public Function UnMute() As String
        UnMute = "[" & Chr(34) & "ramp" & Chr(34) & ",{" & Chr(34) & "type" & Chr(34) & ":" & Chr(34) & "VOLUME" & Chr(34) & "," & Chr(34) & "cmd_id" & Chr(34) & ":" & CmdID & "," & Chr(34) & "muted" & Chr(34) & ":false}]"
        CmdID = CmdID + 1
    End Function

    Public Function Volume(VolumeLevel As String) As String
        Volume = "[" & Chr(34) & "ramp" & Chr(34) & ",{" & Chr(34) & "volume" & Chr(34) & ":" & VolumeLevel & "," & Chr(34) & "type" & Chr(34) & ":" & Chr(34) & "VOLUME" & Chr(34) & "," & Chr(34) & "cmd_id" & Chr(34) & ":" & CmdID & "}]"
        CmdID = CmdID + 1
    End Function

    Public Function GetUnixTimestamp() As Double
        Return (DateTime.Now - New DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalSeconds
    End Function

    Public Function FileInUse(ByVal sFile As String) As Boolean
        If System.IO.File.Exists(sFile) Then
            Try
                Dim F As Short = FreeFile()
                FileOpen(F, sFile, OpenMode.Binary, OpenAccess.ReadWrite, OpenShare.LockReadWrite)
                FileClose(F)
            Catch
                Return True
            End Try
        End If
        Return False
    End Function

    Public Function MD5Hash(ByVal StrText As String) As String
        Dim md5 As New MD5CryptoServiceProvider()
        Dim result As Byte()
        result = md5.ComputeHash(Encoding.ASCII.GetBytes(StrText))
        Dim strBuilder As New StringBuilder()
        For i As Integer = 0 To result.Length - 1
            strBuilder.Append(result(i).ToString("x2"))
        Next
        Return strBuilder.ToString()
    End Function

    Public Function GetFile(ByVal StrFileName As String) As String
        Dim H2 As Integer = FileSystem.FreeFile()
        FileSystem.FileOpen(H2, StrFileName, OpenMode.Binary)
        Dim fileLength As Integer = FileSystem.LOF(H2)
        GetFile = New String(Chr(0), fileLength)
        FileSystem.FileGet(H2, GetFile)
        FileSystem.FileClose(H2)
    End Function

End Module