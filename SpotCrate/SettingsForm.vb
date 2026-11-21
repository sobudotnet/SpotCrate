Imports System.Diagnostics
Imports System.Drawing
Imports System.IO
Imports System.Reflection
Imports System.Windows.Forms

Public Class SettingsForm
    Inherits Form

    Private Const SPOTIFY_GREEN As Integer = &H1ED760
    Private Const DARK_BG As Integer = &HF0F0F
    Private Const CARD_BG As Integer = &H282828
    Private Const TEXT_PRIMARY As Integer = &HFFFFFF
    Private Const TEXT_SECONDARY As Integer = &HB3B3B3

    Private Const ProjectUrl As String = "https://github.com/sobudotnet/SpotYTResolver"
    Private Const ProjectApiUrl As String = "https://api.github.com/repos/sobudotnet/SpotYTResolver"

    Private txtDownloadLocation As TextBox
    Private chkBulkDownloads As CheckBox
    Private btnWipeCredentials As Button
    Private lblCredentialStatus As Label
    Private _credentialsCleared As Boolean

    Public ReadOnly Property CredentialsCleared As Boolean
        Get
            Return _credentialsCleared
        End Get
    End Property

    Public Sub New()
        InitializeComponent()
    End Sub

    Private Sub InitializeComponent()
        Me.Text = "SpotCrate Settings"
        Me.Size = New Size(700, 600)
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
            .Text = "Settings",
            .Font = New Font("Segoe UI", 14, FontStyle.Bold),
            .Location = New Point(20, 15),
            .Size = New Size(640, 30),
            .ForeColor = GetColor(SPOTIFY_GREEN)
        }
        headerPanel.Controls.Add(titleLabel)

        Dim subtitleLabel = New Label With {
            .Text = "Manage downloads, credentials, and project information",
            .Font = New Font("Segoe UI", 9),
            .Location = New Point(20, 48),
            .Size = New Size(640, 20),
            .ForeColor = GetColor(TEXT_SECONDARY)
        }
        headerPanel.Controls.Add(subtitleLabel)

        Dim downloadLabel = New Label With {
            .Text = "Download location",
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .Location = New Point(20, 100),
            .Size = New Size(300, 25),
            .ForeColor = GetColor(TEXT_PRIMARY)
        }
        Me.Controls.Add(downloadLabel)

        txtDownloadLocation = New TextBox With {
            .Text = ConfigManager.GetDownloadDirectory(),
            .Location = New Point(20, 130),
            .Size = New Size(520, 36),
            .Font = New Font("Segoe UI", 9),
            .BackColor = GetColor(CARD_BG),
            .ForeColor = GetColor(TEXT_PRIMARY),
            .BorderStyle = BorderStyle.FixedSingle
        }
        Me.Controls.Add(txtDownloadLocation)

        Dim browseButton = New Button With {
            .Text = "Browse...",
            .Location = New Point(550, 130),
            .Size = New Size(110, 36),
            .Font = New Font("Segoe UI", 9, FontStyle.Bold),
            .BackColor = Color.FromArgb(&H404040),
            .ForeColor = GetColor(TEXT_PRIMARY),
            .FlatStyle = FlatStyle.Flat
        }
        browseButton.FlatAppearance.BorderSize = 1
        browseButton.FlatAppearance.BorderColor = Color.FromArgb(&H666666)
        AddHandler browseButton.Click, AddressOf BrowseButton_Click
        Me.Controls.Add(browseButton)

        Dim downloadNote = New Label With {
            .Text = "New downloads will be saved here. Existing files will not be moved.",
            .Font = New Font("Segoe UI", 9),
            .Location = New Point(20, 175),
            .Size = New Size(640, 25),
            .ForeColor = GetColor(TEXT_SECONDARY)
        }
        Me.Controls.Add(downloadNote)

        chkBulkDownloads = New CheckBox With {
            .Text = "Enable bulk downloads",
            .Checked = ConfigManager.GetBulkDownloadsEnabled(),
            .AutoSize = True,
            .Location = New Point(20, 205),
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .BackColor = GetColor(DARK_BG),
            .ForeColor = GetColor(TEXT_PRIMARY),
            .FlatStyle = FlatStyle.Standard
        }
        Me.Controls.Add(chkBulkDownloads)

        Dim bulkDownloadNote = New Label With {
            .Text = "Allows multiple Spotify links to be queued and downloaded one after another.",
            .Font = New Font("Segoe UI", 9),
            .Location = New Point(40, 232),
            .Size = New Size(620, 22),
            .ForeColor = GetColor(TEXT_SECONDARY)
        }
        Me.Controls.Add(bulkDownloadNote)

        Dim credentialsLabel = New Label With {
            .Text = "Spotify credentials",
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .Location = New Point(20, 270),
            .Size = New Size(300, 25),
            .ForeColor = GetColor(TEXT_PRIMARY)
        }
        Me.Controls.Add(credentialsLabel)

        btnWipeCredentials = New Button With {
            .Text = "Wipe Credentials",
            .Location = New Point(20, 300),
            .Size = New Size(170, 36),
            .Font = New Font("Segoe UI", 9, FontStyle.Bold),
            .BackColor = GetColor(&H663333),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .TabStop = False,
            .UseVisualStyleBackColor = False
        }
        btnWipeCredentials.FlatAppearance.BorderSize = 1
        btnWipeCredentials.FlatAppearance.BorderColor = GetColor(&H994444)
        btnWipeCredentials.FlatAppearance.MouseOverBackColor = GetColor(&H7A3D3D)
        btnWipeCredentials.FlatAppearance.MouseDownBackColor = GetColor(&H542727)
        AddHandler btnWipeCredentials.Click, AddressOf WipeCredentialsButton_Click
        Me.Controls.Add(btnWipeCredentials)

        lblCredentialStatus = New Label With {
            .Text = "This clears the saved Spotify Client ID and Client Secret.",
            .Font = New Font("Segoe UI", 9),
            .Location = New Point(205, 308),
            .Size = New Size(455, 25),
            .ForeColor = GetColor(TEXT_SECONDARY),
            .AutoEllipsis = True
        }
        Me.Controls.Add(lblCredentialStatus)

        Dim aboutLabel = New Label With {
            .Text = "About SpotCrate",
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .Location = New Point(20, 355),
            .Size = New Size(300, 25),
            .ForeColor = GetColor(TEXT_PRIMARY)
        }
        Me.Controls.Add(aboutLabel)

        Dim versionLabel = New Label With {
            .Text = GetVersionText(),
            .Font = New Font("Segoe UI", 9),
            .Location = New Point(20, 388),
            .Size = New Size(640, 22),
            .ForeColor = GetColor(TEXT_SECONDARY)
        }
        Me.Controls.Add(versionLabel)

        Dim projectCaption = New Label With {
            .Text = "Project:",
            .Font = New Font("Segoe UI", 9),
            .Location = New Point(20, 418),
            .Size = New Size(60, 22),
            .ForeColor = GetColor(TEXT_SECONDARY)
        }
        Me.Controls.Add(projectCaption)

        Dim projectLink = CreateLinkLabel(ProjectUrl, New Point(80, 418), 580)
        AddHandler projectLink.LinkClicked, Sub(sender As Object, e As LinkLabelLinkClickedEventArgs)
                                                OpenLink(ProjectUrl)
                                            End Sub
        Me.Controls.Add(projectLink)

        Dim apiCaption = New Label With {
            .Text = "GitHub API:",
            .Font = New Font("Segoe UI", 9),
            .Location = New Point(20, 448),
            .Size = New Size(80, 22),
            .ForeColor = GetColor(TEXT_SECONDARY)
        }
        Me.Controls.Add(apiCaption)

        Dim apiLink = CreateLinkLabel(ProjectApiUrl, New Point(100, 448), 560)
        AddHandler apiLink.LinkClicked, Sub(sender As Object, e As LinkLabelLinkClickedEventArgs)
                                            OpenLink(ProjectApiUrl)
                                        End Sub
        Me.Controls.Add(apiLink)

        Dim footerPanel = New Panel With {
            .Dock = DockStyle.Bottom,
            .Height = 72,
            .BackColor = GetColor(CARD_BG),
            .Padding = New Padding(20, 10, 20, 8)
        }
        Me.Controls.Add(footerPanel)

        Dim footerActions = New FlowLayoutPanel With {
            .Dock = DockStyle.Right,
            .Width = 260,
            .FlowDirection = FlowDirection.LeftToRight,
            .WrapContents = False,
            .Padding = New Padding(0),
            .Margin = New Padding(0),
            .BackColor = Color.Transparent
        }

        Dim saveButton = New Button With {
            .Text = "Save Settings",
            .Size = New Size(125, 35),
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .BackColor = GetColor(SPOTIFY_GREEN),
            .ForeColor = Color.Black,
            .FlatStyle = FlatStyle.Flat,
            .Margin = New Padding(0)
        }
        saveButton.FlatAppearance.BorderSize = 0
        AddHandler saveButton.Click, AddressOf SaveButton_Click
        footerActions.Controls.Add(saveButton)

        Dim cancelButton = New Button With {
            .Text = "Cancel",
            .Size = New Size(120, 35),
            .Font = New Font("Segoe UI", 10),
            .BackColor = Color.FromArgb(&H404040),
            .ForeColor = GetColor(TEXT_PRIMARY),
            .FlatStyle = FlatStyle.Flat,
            .DialogResult = DialogResult.Cancel,
            .Margin = New Padding(15, 0, 0, 0)
        }
        cancelButton.FlatAppearance.BorderSize = 1
        cancelButton.FlatAppearance.BorderColor = Color.FromArgb(&H666666)
        footerActions.Controls.Add(cancelButton)
        footerPanel.Controls.Add(footerActions)

        Dim attributionLabel = New Label With {
            .Text = "with love, mira.",
            .Font = New Font("Segoe UI", 8, FontStyle.Italic),
            .Location = New Point(20, 48),
            .Size = New Size(200, 18),
            .ForeColor = GetColor(TEXT_SECONDARY)
        }
        footerPanel.Controls.Add(attributionLabel)

        Me.AcceptButton = saveButton
        Me.CancelButton = cancelButton
    End Sub

    Private Sub BrowseButton_Click(sender As Object, e As EventArgs)
        Using dialog As New FolderBrowserDialog()
            dialog.Description = "Choose where downloaded MP3 files should be saved"
            dialog.SelectedPath = If(Directory.Exists(txtDownloadLocation.Text), txtDownloadLocation.Text, ConfigManager.GetDefaultDownloadDirectory())

            If dialog.ShowDialog(Me) = DialogResult.OK Then
                txtDownloadLocation.Text = dialog.SelectedPath
            End If
        End Using
    End Sub

    Private Sub WipeCredentialsButton_Click(sender As Object, e As EventArgs)
        Dim confirmation = MessageBox.Show(
            "This will remove the saved Spotify Client ID and Client Secret from this computer. Continue?",
            "Wipe Spotify Credentials",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning)

        If confirmation <> DialogResult.Yes Then
            Return
        End If

        Try
            ConfigManager.ClearCredentials()
            _credentialsCleared = True
            btnWipeCredentials.Enabled = False
            lblCredentialStatus.Text = "Spotify credentials have been wiped."
            lblCredentialStatus.ForeColor = GetColor(SPOTIFY_GREEN)
            MessageBox.Show("Spotify credentials were removed.", "Credentials Wiped", MessageBoxButtons.OK, MessageBoxIcon.Information)
        Catch ex As Exception
            MessageBox.Show(ex.Message, "Could Not Wipe Credentials", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub SaveButton_Click(sender As Object, e As EventArgs)
        If String.IsNullOrWhiteSpace(txtDownloadLocation.Text) Then
            MessageBox.Show("Please choose a download location.", "Download Location Required", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Try
            Dim downloadDirectory = Path.GetFullPath(txtDownloadLocation.Text.Trim())
            Directory.CreateDirectory(downloadDirectory)
            ConfigManager.SaveDownloadDirectory(downloadDirectory)
            ConfigManager.SaveBulkDownloadsEnabled(chkBulkDownloads.Checked)
            Me.DialogResult = DialogResult.OK
            Me.Close()
        Catch ex As Exception
            MessageBox.Show($"Could not save settings: {ex.Message}", "Settings Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Function CreateLinkLabel(text As String, location As Point, width As Integer) As LinkLabel
        Return New LinkLabel With {
            .Text = text,
            .Font = New Font("Segoe UI", 9),
            .Location = location,
            .Size = New Size(width, 22),
            .LinkColor = GetColor(SPOTIFY_GREEN),
            .ActiveLinkColor = Color.Yellow,
            .VisitedLinkColor = GetColor(SPOTIFY_GREEN),
            .AutoEllipsis = True
        }
    End Function

    Private Shared Function GetVersionText() As String
        Dim version = Assembly.GetExecutingAssembly().GetName().Version
        If version Is Nothing Then
            Return "Version unknown"
        End If

        Return $"Version {version.Major}.{version.Minor}.{version.Build}"
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
