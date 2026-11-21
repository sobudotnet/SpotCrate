Imports System.Diagnostics
Imports System.IO
Imports System.Threading.Tasks

Public NotInheritable Class YouTubeDownloadService
    Private Const YtDlpFileName As String = "yt-dlp.exe"
    Private Const FfmpegFileName As String = "ffmpeg.exe"

    Private Sub New()
    End Sub

    Public Shared ReadOnly Property ToolsDirectory As String
        Get
            Return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tools")
        End Get
    End Property

    Public Shared ReadOnly Property DownloadDirectory As String
        Get
            Return ConfigManager.GetDownloadDirectory()
        End Get
    End Property

    Public Shared Function GetMissingToolsMessage() As String
        Dim missingTools = New List(Of String)()

        If Not File.Exists(Path.Combine(ToolsDirectory, YtDlpFileName)) Then
            missingTools.Add(YtDlpFileName)
        End If

        If Not File.Exists(Path.Combine(ToolsDirectory, FfmpegFileName)) Then
            missingTools.Add(FfmpegFileName)
        End If

        If missingTools.Count = 0 Then
            Return String.Empty
        End If

        Return $"Add {String.Join(" and ", missingTools)} to '{ToolsDirectory}' before downloading."
    End Function

    Public Shared Async Function DownloadAudioAsync(videoUrl As String) As Task(Of String)
        If String.IsNullOrWhiteSpace(videoUrl) Then
            Throw New ArgumentException("A YouTube URL is required.", NameOf(videoUrl))
        End If

        Dim missingToolsMessage = GetMissingToolsMessage()
        If Not String.IsNullOrEmpty(missingToolsMessage) Then
            Throw New InvalidOperationException(missingToolsMessage)
        End If

        Directory.CreateDirectory(DownloadDirectory)

        Dim outputTemplate = Path.Combine(DownloadDirectory, "%(title)s.%(ext)s")
        Dim startInfo = New ProcessStartInfo With {
            .FileName = Path.Combine(ToolsDirectory, YtDlpFileName),
            .UseShellExecute = False,
            .CreateNoWindow = True,
            .RedirectStandardOutput = True,
            .RedirectStandardError = True,
            .WorkingDirectory = ToolsDirectory
        }

        startInfo.ArgumentList.Add("--no-playlist")
        startInfo.ArgumentList.Add("--extract-audio")
        startInfo.ArgumentList.Add("--audio-format")
        startInfo.ArgumentList.Add("mp3")
        startInfo.ArgumentList.Add("--audio-quality")
        startInfo.ArgumentList.Add("0")
        startInfo.ArgumentList.Add("--windows-filenames")
        startInfo.ArgumentList.Add("--ffmpeg-location")
        startInfo.ArgumentList.Add(ToolsDirectory)
        startInfo.ArgumentList.Add("--output")
        startInfo.ArgumentList.Add(outputTemplate)
        startInfo.ArgumentList.Add("--print")
        startInfo.ArgumentList.Add("after_move:filepath")
        startInfo.ArgumentList.Add(videoUrl)

        Using process As New Process()
            process.StartInfo = startInfo

            If Not process.Start() Then
                Throw New InvalidOperationException("Could not start yt-dlp.")
            End If

            Dim outputTask = process.StandardOutput.ReadToEndAsync()
            Dim errorTask = process.StandardError.ReadToEndAsync()

            Await process.WaitForExitAsync()

            Dim standardOutput = Await outputTask
            Dim standardError = Await errorTask

            If process.ExitCode <> 0 Then
                Dim details = standardError.Trim()
                If String.IsNullOrEmpty(details) Then
                    details = standardOutput.Trim()
                End If
                If String.IsNullOrEmpty(details) Then
                    details = "yt-dlp did not provide an error message."
                End If

                Throw New InvalidOperationException($"yt-dlp failed ({process.ExitCode}): {details}")
            End If

            Dim downloadedFile = FindDownloadedFile(standardOutput)
            If String.IsNullOrEmpty(downloadedFile) Then
                Throw New InvalidOperationException("yt-dlp completed, but the downloaded MP3 file could not be located.")
            End If

            Return downloadedFile
        End Using
    End Function

    Private Shared Function FindDownloadedFile(standardOutput As String) As String
        Dim outputLines = standardOutput.Split({Environment.NewLine, vbLf}, StringSplitOptions.RemoveEmptyEntries)

        For index As Integer = outputLines.Length - 1 To 0 Step -1
            Dim candidate = outputLines(index).Trim().Trim(""""c)
            If File.Exists(candidate) AndAlso String.Equals(Path.GetExtension(candidate), ".mp3", StringComparison.OrdinalIgnoreCase) Then
                Return candidate
            End If
        Next

        Return String.Empty
    End Function
End Class
