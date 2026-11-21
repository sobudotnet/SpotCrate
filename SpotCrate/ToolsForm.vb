Imports System.Diagnostics
Imports System.Drawing
Imports System.Windows.Forms

Public Class ToolsForm
    Inherits Form

    Private Const SPOTIFY_GREEN As Integer = &H1ED760
    Private Const DARK_BG As Integer = &HF0F0F
    Private Const CARD_BG As Integer = &H282828
    Private Const TEXT_PRIMARY As Integer = &HFFFFFF
    Private Const TEXT_SECONDARY As Integer = &HB3B3B3

    Private Const YtDlpDownloadUrl As String = "https://github.com/yt-dlp/yt-dlp/releases/latest"
    Private Const FfmpegDownloadUrl As String = "https://ffmpeg.org/download.html"

    Public Sub New()
        InitializeComponent()
    End Sub

    Private Sub InitializeComponent()
        Me.Text = "SpotCrate Tools"
        Me.Size = New Size(700, 470)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.BackColor = GetColor(DARK_BG)
        Me.ForeColor = GetColor(TEXT_PRIMARY)
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.DoubleBuffered = True

        Dim headerPanel = New Panel With {
            .Dock = DockStyle.Top,
            .Height = 80,
            .BackColor = GetColor(CARD_BG)
        }
        Me.Controls.Add(headerPanel)

        Dim titleLabel = New Label With {
            .Text = "Download Tools",
            .Font = New Font("Segoe UI", 14, FontStyle.Bold),
            .Location = New Point(20, 15),
            .Size = New Size(640, 30),
            .ForeColor = GetColor(SPOTIFY_GREEN)
        }
        headerPanel.Controls.Add(titleLabel)

        Dim subtitleLabel = New Label With {
            .Text = "Install these tools to enable MP3 downloads",
            .Font = New Font("Segoe UI", 9),
            .Location = New Point(20, 48),
            .Size = New Size(640, 20),
            .ForeColor = GetColor(TEXT_SECONDARY)
        }
        headerPanel.Controls.Add(subtitleLabel)

        Dim instructionsLabel = New Label With {
            .Text = "Download both Windows executables, then place them in this folder:",
            .Font = New Font("Segoe UI", 10),
            .Location = New Point(20, 100),
            .Size = New Size(640, 25),
            .ForeColor = GetColor(TEXT_PRIMARY)
        }
        Me.Controls.Add(instructionsLabel)

        Dim pathTextBox = New TextBox With {
            .Text = YouTubeDownloadService.ToolsDirectory,
            .Location = New Point(20, 130),
            .Size = New Size(640, 34),
            .Font = New Font("Segoe UI", 9),
            .BackColor = GetColor(CARD_BG),
            .ForeColor = GetColor(TEXT_PRIMARY),
            .BorderStyle = BorderStyle.FixedSingle,
            .ReadOnly = True
        }
        Me.Controls.Add(pathTextBox)

        Dim ytDlpLabel = New Label With {
            .Text = "1. yt-dlp.exe",
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .Location = New Point(20, 185),
            .Size = New Size(200, 25),
            .ForeColor = GetColor(TEXT_PRIMARY)
        }
        Me.Controls.Add(ytDlpLabel)

        Dim ytDlpLink = CreateLinkLabel("Open yt-dlp releases", New Point(20, 212))
        AddHandler ytDlpLink.LinkClicked, Sub(sender As Object, e As LinkLabelLinkClickedEventArgs)
                                               OpenLink(YtDlpDownloadUrl)
                                           End Sub
        Me.Controls.Add(ytDlpLink)

        Dim ffmpegLabel = New Label With {
            .Text = "2. ffmpeg.exe",
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .Location = New Point(350, 185),
            .Size = New Size(200, 25),
            .ForeColor = GetColor(TEXT_PRIMARY)
        }
        Me.Controls.Add(ffmpegLabel)

        Dim ffmpegLink = CreateLinkLabel("Open FFmpeg downloads", New Point(350, 212))
        AddHandler ffmpegLink.LinkClicked, Sub(sender As Object, e As LinkLabelLinkClickedEventArgs)
                                                OpenLink(FfmpegDownloadUrl)
                                            End Sub
        Me.Controls.Add(ffmpegLink)

        Dim noteLabel = New Label With {
            .Text = "After copying the files, restart SpotCrate if it was already open. The Download button will then save MP3 files to your configured download folder.",
            .Font = New Font("Segoe UI", 9),
            .Location = New Point(20, 270),
            .Size = New Size(640, 50),
            .ForeColor = GetColor(TEXT_SECONDARY),
            .AutoSize = False
        }
        Me.Controls.Add(noteLabel)

        Dim footerPanel = New Panel With {
            .Dock = DockStyle.Bottom,
            .Height = 65,
            .BackColor = GetColor(CARD_BG)
        }
        Me.Controls.Add(footerPanel)

        Dim closeButton = New Button With {
            .Text = "Close",
            .Location = New Point(555, 15),
            .Size = New Size(105, 35),
            .Font = New Font("Segoe UI", 10),
            .BackColor = Color.FromArgb(&H404040),
            .ForeColor = GetColor(TEXT_PRIMARY),
            .FlatStyle = FlatStyle.Flat,
            .DialogResult = DialogResult.Cancel
        }
        closeButton.FlatAppearance.BorderSize = 1
        closeButton.FlatAppearance.BorderColor = Color.FromArgb(&H666666)
        footerPanel.Controls.Add(closeButton)

        Me.AcceptButton = closeButton
        Me.CancelButton = closeButton
    End Sub

    Private Function CreateLinkLabel(text As String, location As Point) As LinkLabel
        Return New LinkLabel With {
            .Text = text,
            .Font = New Font("Segoe UI", 9),
            .Location = location,
            .Size = New Size(260, 25),
            .LinkColor = GetColor(SPOTIFY_GREEN),
            .ActiveLinkColor = Color.Yellow,
            .VisitedLinkColor = GetColor(SPOTIFY_GREEN)
        }
    End Function

    Private Shared Sub OpenLink(url As String)
        Try
            Dim psi = New ProcessStartInfo With {
                .FileName = url,
                .UseShellExecute = True
            }
            Process.Start(psi)
        Catch ex As Exception
            MessageBox.Show($"Could not open the browser. Visit this URL manually:{Environment.NewLine}{url}", "Browser Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End Try
    End Sub

    Private Shared Function GetColor(value As Integer) As Color
        Return Color.FromArgb((value >> 16) And &HFF, (value >> 8) And &HFF, value And &HFF)
    End Function
End Class
