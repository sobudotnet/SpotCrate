Imports System.Windows.Forms
Imports System.Drawing
Imports System.Configuration
Imports System.IO
Imports System.Text.RegularExpressions
Imports System.Threading.Tasks

Public Class MainForm
    Inherits Form

    Private WithEvents lblTitle As Label
    Private WithEvents lblInstruction As Label
    Private WithEvents txtSpotifyUrl As TextBox
    Private WithEvents btnSearch As Button
    Private WithEvents btnDownloadFirst As Button
    Private WithEvents btnTools As Button
    Private WithEvents btnSettings As Button
    Private WithEvents pnlResults As Panel
    Private WithEvents pnlQueue As Panel
    Private WithEvents pnlQueueItems As Panel
    Private WithEvents btnStartQueue As Button
    Private WithEvents lblQueueTitle As Label
    Private WithEvents lblStatus As Label
    Private WithEvents lblLoading As Label
    Private lblSetupError As Label
    Private pnlStatusBar As Panel
    Private lblStatusMessage As Label
    Private lblDebugMessage As Label
    Private _spotifyAuthService As SpotifyAuthService
    Private _spotifyTrackService As SpotifyTrackService
    Private _isInitialized As Boolean = False
    Private _bulkDownloadsEnabled As Boolean = False
    Private _isQueueRunning As Boolean = False
    Private ReadOnly _queueItems As New List(Of QueueItem)()

    Private Class QueueItem
        Public Property SpotifyUrl As String
        Public Property RowPanel As Panel
        Public Property StatusLabel As Label
        Public Property DeleteButton As Button
        Public Property Completed As Boolean
    End Class

    Private Const SPOTIFY_GREEN As Integer = &H1ED760
    Private Const YOUTUBE_RED As Integer = &HFF0000
    Private Const DARK_BG As Integer = &HF0F0F
    Private Const LIGHT_BG As Integer = &H181818
    Private Const CARD_BG As Integer = &H282828
    Private Const TEXT_PRIMARY As Integer = &HFFFFFF
    Private Const TEXT_SECONDARY As Integer = &HB3B3B3

    Public Sub New()
        InitializeComponent()
        SetupDragAndDrop()
        ConfigManager.Initialize()
        _bulkDownloadsEnabled = ConfigManager.GetBulkDownloadsEnabled()
        UpdateBulkUi()
        SetApplicationIcon()
    End Sub

    Private Sub SetApplicationIcon()
        Try
            Dim iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "spotcrate.ico")
            If System.IO.File.Exists(iconPath) Then
                Me.Icon = New System.Drawing.Icon(iconPath)
            End If
        Catch ex As Exception
            Console.WriteLine($"Icon load error: {ex.Message}")
        End Try
    End Sub

    Private Sub MainForm_Load(sender As Object, e As EventArgs) Handles Me.Load
        InitializeSpotifyAsync()
    End Sub

    Private Async Sub InitializeSpotifyAsync()
        Try

            Dim creds = ConfigManager.LoadCredentials()

            If String.IsNullOrEmpty(creds.ClientId) OrElse String.IsNullOrEmpty(creds.ClientSecret) Then
                ShowSetupError("Spotify API credentials not configured")
                ShowSetupDialog()
                Return
            End If

            _spotifyAuthService = SpotifyAuthService.GetInstance(creds.ClientId, creds.ClientSecret)
            Await _spotifyAuthService.InitializeAsync()
            _spotifyTrackService = New SpotifyTrackService(_spotifyAuthService)
            _isInitialized = True
            HideSetupError()

        Catch ex As Exception
            ShowSetupError($"Failed to initialize Spotify: {ex.Message}")
        End Try
    End Sub

    Private Sub ShowSetupDialog()
        Dim setupForm = New SetupForm()
        If setupForm.ShowDialog() = DialogResult.OK Then
            ConfigManager.SaveCredentials(setupForm.ClientId, setupForm.ClientSecret)
            SpotifyAuthService.Reset()
            _spotifyAuthService = Nothing
            _spotifyTrackService = Nothing
            _isInitialized = False
            ' Reinitialize with new creds
            InitializeSpotifyAsync()
        End If
    End Sub

    Private Sub BtnTools_Click(sender As Object, e As EventArgs)
        Using toolsForm As New ToolsForm()
            toolsForm.ShowDialog(Me)
        End Using
    End Sub

    Private Sub BtnSettings_Click(sender As Object, e As EventArgs)
        Using settingsForm As New SettingsForm()
            settingsForm.ShowDialog(Me)

            If settingsForm.CredentialsCleared Then
                SpotifyAuthService.Reset()
                _spotifyAuthService = Nothing
                _spotifyTrackService = Nothing
                _isInitialized = False
                ShowSetupError("Spotify credentials were removed. Configure credentials to search again.")
            End If

            _bulkDownloadsEnabled = ConfigManager.GetBulkDownloadsEnabled()
            UpdateBulkUi()
        End Using
    End Sub

    Private Sub ShowSetupError(message As String)
        lblSetupError.Text = message
        lblSetupError.Visible = True
        btnSearch.Enabled = False
        btnDownloadFirst.Enabled = False
        UpdateQueueStartButton()
        UpdateDebugMessage("Setup Error: " & message, Color.Red)
    End Sub

    Private Sub HideSetupError()
        lblSetupError.Visible = False
        btnSearch.Enabled = True
        btnDownloadFirst.Enabled = True
        UpdateQueueStartButton()
        UpdateDebugMessage("Ready", Color.FromArgb(30, 200, 96))
    End Sub

    Private Sub UpdateDebugMessage(message As String, Optional color As Color = Nothing)
        If Me.InvokeRequired Then
            Me.Invoke(Sub() UpdateDebugMessage(message, color))
            Return
        End If

        lblDebugMessage.Text = message
        If color <> Nothing Then
            lblDebugMessage.ForeColor = color
        End If
    End Sub

    Private Sub InitializeComponent()
        ' main ting
        Me.Text = "SpotCrate"
        Me.Size = New Size(1000, 900)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.FormBorderStyle = FormBorderStyle.FixedSingle
        Me.MaximizeBox = False
        Me.MinimumSize = Me.Size
        Me.MaximumSize = Me.Size
        Me.BackColor = Color.FromArgb((DARK_BG >> 16) And &HFF, (DARK_BG >> 8) And &HFF, DARK_BG And &HFF)
        Me.ForeColor = Color.FromArgb((TEXT_PRIMARY >> 16) And &HFF, (TEXT_PRIMARY >> 8) And &HFF, TEXT_PRIMARY And &HFF)
        Me.DoubleBuffered = True

        ' title label
        lblTitle = New Label With {
            .Text = "SpotCrate",
            .Font = New Font("Segoe UI", 18, FontStyle.Bold),
            .AutoSize = True,
            .Location = New Point(25, 20),
            .ForeColor = Color.FromArgb((SPOTIFY_GREEN >> 16) And &HFF, (SPOTIFY_GREEN >> 8) And &HFF, SPOTIFY_GREEN And &HFF)
        }
        Me.Controls.Add(lblTitle)

        ' label
        Dim lblSubtitle = New Label With {
            .Text = "Spotify YouTube Resolver",
            .Font = New Font("Segoe UI", 9),
            .AutoSize = True,
            .Location = New Point(25, 50),
            .ForeColor = Color.FromArgb((TEXT_SECONDARY >> 16) And &HFF, (TEXT_SECONDARY >> 8) And &HFF, TEXT_SECONDARY And &HFF)
        }
        Me.Controls.Add(lblSubtitle)

        ' inst
        lblInstruction = New Label With {
            .Text = "Drag a Spotify link here or paste it below to find it on YouTube",
            .Font = New Font("Segoe UI", 10),
            .AutoSize = True,
            .Location = New Point(25, 73),
            .ForeColor = Color.FromArgb((TEXT_SECONDARY >> 16) And &HFF, (TEXT_SECONDARY >> 8) And &HFF, TEXT_SECONDARY And &HFF)
        }
        Me.Controls.Add(lblInstruction)

        ' url input box
        txtSpotifyUrl = New TextBox With {
            .Location = New Point(25, 103),
            .Size = New Size(750, 45),
            .Font = New Font("Segoe UI", 11),
            .BackColor = Color.FromArgb((CARD_BG >> 16) And &HFF, (CARD_BG >> 8) And &HFF, CARD_BG And &HFF),
            .ForeColor = Color.FromArgb((TEXT_PRIMARY >> 16) And &HFF, (TEXT_PRIMARY >> 8) And &HFF, TEXT_PRIMARY And &HFF),
            .AllowDrop = True,
            .Multiline = False,
            .BorderStyle = BorderStyle.FixedSingle
        }
        Me.Controls.Add(txtSpotifyUrl)

        ' YouTube search button
        btnSearch = New Button With {
            .Text = "YT",
            .Location = New Point(785, 103),
            .Size = New Size(90, 45),
            .Font = New Font("Segoe UI", 11, FontStyle.Bold),
            .BackColor = Color.FromArgb((YOUTUBE_RED >> 16) And &HFF, (YOUTUBE_RED >> 8) And &HFF, YOUTUBE_RED And &HFF),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .TabStop = False,
            .UseVisualStyleBackColor = False
        }
        btnSearch.FlatAppearance.BorderSize = 0
        btnSearch.FlatAppearance.MouseOverBackColor = Color.FromArgb(&HCC, 0, 0)
        btnSearch.FlatAppearance.MouseDownBackColor = Color.FromArgb(&H990000)
        Me.Controls.Add(btnSearch)

        ' first-result download button
        btnDownloadFirst = New Button With {
            .Text = "DL",
            .Location = New Point(885, 103),
            .Size = New Size(90, 45),
            .Font = New Font("Segoe UI", 11, FontStyle.Bold),
            .BackColor = Color.FromArgb((SPOTIFY_GREEN >> 16) And &HFF, (SPOTIFY_GREEN >> 8) And &HFF, SPOTIFY_GREEN And &HFF),
            .ForeColor = Color.Black,
            .FlatStyle = FlatStyle.Flat,
            .TabStop = False,
            .UseVisualStyleBackColor = False
        }
        btnDownloadFirst.FlatAppearance.BorderSize = 0
        btnDownloadFirst.FlatAppearance.MouseOverBackColor = Color.FromArgb(&H2C, &HD6, &H60)
        btnDownloadFirst.FlatAppearance.MouseDownBackColor = Color.FromArgb(&H1A, &H9D, &H48)
        Me.Controls.Add(btnDownloadFirst)

        ' setup err
        lblSetupError = New Label With {
            .Text = "",
            .Font = New Font("Segoe UI", 10),
            .Location = New Point(25, 155),
            .Size = New Size(950, 35),
            .ForeColor = Color.FromArgb(&HFF, &H4D, &H4D),
            .Visible = False,
            .AutoSize = False,
            .BackColor = Color.FromArgb(&H331111)
        }
        Me.Controls.Add(lblSetupError)

        ' loading lbl
        lblLoading = New Label With {
            .Text = "Searching...",
            .Font = New Font("Segoe UI", 11),
            .Location = New Point(25, 200),
            .Size = New Size(950, 30),
            .ForeColor = Color.FromArgb((SPOTIFY_GREEN >> 16) And &HFF, (SPOTIFY_GREEN >> 8) And &HFF, SPOTIFY_GREEN And &HFF),
            .Visible = False,
            .TextAlign = ContentAlignment.MiddleCenter
        }
        Me.Controls.Add(lblLoading)

        pnlQueue = New Panel With {
            .Location = New Point(25, 235),
            .Size = New Size(950, 160),
            .BackColor = Color.FromArgb((LIGHT_BG >> 16) And &HFF, (LIGHT_BG >> 8) And &HFF, LIGHT_BG And &HFF),
            .BorderStyle = BorderStyle.None,
            .Visible = False
        }
        Me.Controls.Add(pnlQueue)

        lblQueueTitle = New Label With {
            .Text = "Bulk Queue (0)",
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .Location = New Point(10, 8),
            .Size = New Size(300, 25),
            .ForeColor = Color.FromArgb((SPOTIFY_GREEN >> 16) And &HFF, (SPOTIFY_GREEN >> 8) And &HFF, SPOTIFY_GREEN And &HFF)
        }
        pnlQueue.Controls.Add(lblQueueTitle)

        btnStartQueue = New Button With {
            .Text = "Start Queue",
            .Location = New Point(815, 5),
            .Size = New Size(120, 32),
            .Font = New Font("Segoe UI", 9, FontStyle.Bold),
            .BackColor = Color.FromArgb((SPOTIFY_GREEN >> 16) And &HFF, (SPOTIFY_GREEN >> 8) And &HFF, SPOTIFY_GREEN And &HFF),
            .ForeColor = Color.Black,
            .FlatStyle = FlatStyle.Flat,
            .Enabled = False
        }
        btnStartQueue.FlatAppearance.BorderSize = 0
        btnStartQueue.FlatAppearance.MouseOverBackColor = Color.FromArgb(&H2CD660)
        btnStartQueue.FlatAppearance.MouseDownBackColor = Color.FromArgb(&H1A9D48)
        AddHandler btnStartQueue.Click, AddressOf BtnStartQueue_Click
        pnlQueue.Controls.Add(btnStartQueue)

        pnlQueueItems = New Panel With {
            .Location = New Point(10, 45),
            .Size = New Size(930, 105),
            .BackColor = Color.FromArgb((CARD_BG >> 16) And &HFF, (CARD_BG >> 8) And &HFF, CARD_BG And &HFF),
            .AutoScroll = True,
            .BorderStyle = BorderStyle.None,
            .AllowDrop = True
        }
        pnlQueue.Controls.Add(pnlQueueItems)

        ' results
        pnlResults = New Panel With {
            .Location = New Point(25, 235),
            .Size = New Size(950, 530),
            .BackColor = Color.FromArgb((LIGHT_BG >> 16) And &HFF, (LIGHT_BG >> 8) And &HFF, LIGHT_BG And &HFF),
            .AutoScroll = True,
            .BorderStyle = BorderStyle.None
        }
        Me.Controls.Add(pnlResults)

        ' status bar
        pnlStatusBar = New Panel With {
            .Location = New Point(0, Me.Height - 70),
            .Size = New Size(Me.Width, 70),
            .BackColor = Color.FromArgb((CARD_BG >> 16) And &HFF, (CARD_BG >> 8) And &HFF, CARD_BG And &HFF),
            .BorderStyle = BorderStyle.None,
            .Dock = DockStyle.Bottom
        }
        Me.Controls.Add(pnlStatusBar)

        ' status msg
        lblStatusMessage = New Label With {
            .Text = "Status: Initializing...",
            .Font = New Font("Segoe UI", 9),
            .Location = New Point(15, 8),
            .Size = New Size(700, 20),
            .ForeColor = Color.FromArgb((TEXT_SECONDARY >> 16) And &HFF, (TEXT_SECONDARY >> 8) And &HFF, TEXT_SECONDARY And &HFF),
            .AutoEllipsis = True
        }
        pnlStatusBar.Controls.Add(lblStatusMessage)

        ' debug msg lbl
        lblDebugMessage = New Label With {
            .Text = "Ready",
            .Font = New Font("Segoe UI", 9, FontStyle.Bold),
            .Location = New Point(15, 30),
            .Size = New Size(700, 20),
            .ForeColor = Color.FromArgb((SPOTIFY_GREEN >> 16) And &HFF, (SPOTIFY_GREEN >> 8) And &HFF, SPOTIFY_GREEN And &HFF)
        }
        pnlStatusBar.Controls.Add(lblDebugMessage)

        btnTools = New Button With {
            .Text = "Tools",
            .Location = New Point(755, 19),
            .Size = New Size(95, 32),
            .Font = New Font("Segoe UI", 9, FontStyle.Bold),
            .BackColor = Color.FromArgb((CARD_BG >> 16) And &HFF, (CARD_BG >> 8) And &HFF, CARD_BG And &HFF),
            .ForeColor = Color.FromArgb((SPOTIFY_GREEN >> 16) And &HFF, (SPOTIFY_GREEN >> 8) And &HFF, SPOTIFY_GREEN And &HFF),
            .FlatStyle = FlatStyle.Flat
        }
        btnTools.FlatAppearance.BorderSize = 1
        btnTools.FlatAppearance.BorderColor = Color.FromArgb((SPOTIFY_GREEN >> 16) And &HFF, (SPOTIFY_GREEN >> 8) And &HFF, SPOTIFY_GREEN And &HFF)
        btnTools.FlatAppearance.MouseOverBackColor = Color.FromArgb(&H333333)
        btnTools.FlatAppearance.MouseDownBackColor = Color.FromArgb(&H404040)
        AddHandler btnTools.Click, AddressOf BtnTools_Click
        pnlStatusBar.Controls.Add(btnTools)

        btnSettings = New Button With {
            .Text = "Settings",
            .Location = New Point(855, 19),
            .Size = New Size(115, 32),
            .Font = New Font("Segoe UI", 9, FontStyle.Bold),
            .BackColor = Color.FromArgb((CARD_BG >> 16) And &HFF, (CARD_BG >> 8) And &HFF, CARD_BG And &HFF),
            .ForeColor = Color.FromArgb((SPOTIFY_GREEN >> 16) And &HFF, (SPOTIFY_GREEN >> 8) And &HFF, SPOTIFY_GREEN And &HFF),
            .FlatStyle = FlatStyle.Flat
        }
        btnSettings.FlatAppearance.BorderSize = 1
        btnSettings.FlatAppearance.BorderColor = Color.FromArgb((SPOTIFY_GREEN >> 16) And &HFF, (SPOTIFY_GREEN >> 8) And &HFF, SPOTIFY_GREEN And &HFF)
        btnSettings.FlatAppearance.MouseOverBackColor = Color.FromArgb(&H333333)
        btnSettings.FlatAppearance.MouseDownBackColor = Color.FromArgb(&H404040)
        AddHandler btnSettings.Click, AddressOf BtnSettings_Click
        pnlStatusBar.Controls.Add(btnSettings)

        ' handlers
        AddHandler txtSpotifyUrl.DragDrop, AddressOf TxtSpotifyUrl_DragDrop
        AddHandler txtSpotifyUrl.DragOver, AddressOf TxtSpotifyUrl_DragOver
        AddHandler pnlQueue.DragDrop, AddressOf Queue_DragDrop
        AddHandler pnlQueue.DragOver, AddressOf Queue_DragOver
        AddHandler pnlQueueItems.DragDrop, AddressOf Queue_DragDrop
        AddHandler pnlQueueItems.DragOver, AddressOf Queue_DragOver
        AddHandler btnSearch.Click, AddressOf BtnSearch_Click
        AddHandler btnDownloadFirst.Click, AddressOf BtnDownloadFirst_Click
    End Sub

    Private Sub SetupDragAndDrop()
        txtSpotifyUrl.AllowDrop = True
        pnlQueue.AllowDrop = True
        pnlQueueItems.AllowDrop = True
    End Sub

    Private Sub TxtSpotifyUrl_DragOver(sender As Object, e As DragEventArgs)
        If e.Data.GetDataPresent(DataFormats.Text) Then
            e.Effect = DragDropEffects.Copy
        End If
    End Sub

    Private Sub TxtSpotifyUrl_DragDrop(sender As Object, e As DragEventArgs)
        Dim droppedText = CStr(e.Data.GetData(DataFormats.Text))
        If droppedText.Contains("spotify") Then
            txtSpotifyUrl.Text = droppedText.Trim()
            BtnSearch_Click(Nothing, Nothing)
        Else
            MessageBox.Show("Please drop a valid Spotify link", "Invalid Link", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End If
    End Sub

    Private Sub Queue_DragOver(sender As Object, e As DragEventArgs)
        If Not _bulkDownloadsEnabled OrElse _isQueueRunning Then
            e.Effect = DragDropEffects.None
        ElseIf e.Data.GetDataPresent(DataFormats.Text) OrElse e.Data.GetDataPresent(DataFormats.FileDrop) Then
            e.Effect = DragDropEffects.Copy
        Else
            e.Effect = DragDropEffects.None
        End If
    End Sub

    Private Sub Queue_DragDrop(sender As Object, e As DragEventArgs)
        If Not _bulkDownloadsEnabled OrElse _isQueueRunning Then
            Return
        End If

        Dim urls = ExtractDroppedSpotifyUrls(e.Data)
        If urls.Count = 0 Then
            MessageBox.Show("Drop one or more Spotify track links into the queue.", "No Spotify Tracks Found", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        For Each url In urls
            AddQueueItem(url)
        Next

        RefreshQueueUi()
    End Sub

    Private Function ExtractDroppedSpotifyUrls(data As IDataObject) As List(Of String)
        Dim droppedText = New List(Of String)()

        If data.GetDataPresent(DataFormats.Text) Then
            droppedText.Add(CStr(data.GetData(DataFormats.Text)))
        End If

        If data.GetDataPresent(DataFormats.FileDrop) Then
            Dim droppedFiles = TryCast(data.GetData(DataFormats.FileDrop), String())
            If droppedFiles IsNot Nothing Then
                For Each filePath In droppedFiles
                    Try
                        If File.Exists(filePath) Then
                            droppedText.Add(File.ReadAllText(filePath))
                        End If
                    Catch ex As Exception
                        Console.WriteLine($"Could not read dropped file: {ex.Message}")
                    End Try
                Next
            End If
        End If

        Dim urls = New List(Of String)()
        For Each droppedValue As String In droppedText
            Dim matches = Regex.Matches(droppedValue, "(?:https?://open\.spotify\.com/track/[^\s]+|spotify:track:[A-Za-z0-9]+)", RegexOptions.IgnoreCase)
            For Each match As Match In matches
                Dim candidate = match.Value.Trim().TrimEnd("."c, ","c, ";"c)
                If SpotifyUrlParser.ExtractTrackInfo(candidate) IsNot Nothing Then
                    urls.Add(candidate)
                End If
            Next
        Next

        Return urls
    End Function

    Private Sub AddQueueItem(spotifyUrl As String)
        Dim queueItem = New QueueItem With {
            .SpotifyUrl = spotifyUrl,
            .Completed = False
        }

        Dim rowPanel = New Panel With {
            .Size = New Size(910, 38),
            .BackColor = Color.FromArgb((DARK_BG >> 16) And &HFF, (DARK_BG >> 8) And &HFF, DARK_BG And &HFF),
            .BorderStyle = BorderStyle.None
        }

        Dim urlLabel = New Label With {
            .Text = spotifyUrl,
            .Font = New Font("Segoe UI", 9),
            .Location = New Point(10, 8),
            .Size = New Size(610, 22),
            .ForeColor = Color.FromArgb((TEXT_PRIMARY >> 16) And &HFF, (TEXT_PRIMARY >> 8) And &HFF, TEXT_PRIMARY And &HFF),
            .AutoEllipsis = True
        }
        rowPanel.Controls.Add(urlLabel)

        Dim statusLabel = New Label With {
            .Text = "Waiting",
            .Font = New Font("Segoe UI", 8),
            .Location = New Point(625, 9),
            .Size = New Size(150, 20),
            .ForeColor = Color.FromArgb((TEXT_SECONDARY >> 16) And &HFF, (TEXT_SECONDARY >> 8) And &HFF, TEXT_SECONDARY And &HFF),
            .AutoEllipsis = True,
            .TextAlign = ContentAlignment.MiddleRight
        }
        rowPanel.Controls.Add(statusLabel)

        Dim deleteButton = New Button With {
            .Text = "Delete",
            .Location = New Point(790, 4),
            .Size = New Size(90, 30),
            .Font = New Font("Segoe UI", 8, FontStyle.Bold),
            .BackColor = Color.FromArgb(&H663333),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat
        }
        deleteButton.FlatAppearance.BorderSize = 1
        deleteButton.FlatAppearance.BorderColor = Color.FromArgb(&H994444)
        rowPanel.Controls.Add(deleteButton)

        queueItem.RowPanel = rowPanel
        queueItem.StatusLabel = statusLabel
        queueItem.DeleteButton = deleteButton

        AddHandler deleteButton.Click, Sub(sender As Object, e As EventArgs)
                                           RemoveQueueItem(queueItem)
                                       End Sub

        _queueItems.Add(queueItem)
    End Sub

    Private Sub RemoveQueueItem(queueItem As QueueItem)
        If _isQueueRunning Then
            Return
        End If

        If _queueItems.Remove(queueItem) Then
            queueItem.RowPanel.Dispose()
            RefreshQueueUi()
        End If
    End Sub

    Private Sub RefreshQueueUi()
        If pnlQueueItems Is Nothing Then
            Return
        End If

        pnlQueueItems.Controls.Clear()
        Dim yPosition = 5

        If _queueItems.Count = 0 Then
            Dim emptyLabel = New Label With {
                .Text = "Drop Spotify track links here to add them to the queue.",
                .Font = New Font("Segoe UI", 9),
                .Location = New Point(10, 20),
                .Size = New Size(700, 25),
                .ForeColor = Color.FromArgb((TEXT_SECONDARY >> 16) And &HFF, (TEXT_SECONDARY >> 8) And &HFF, TEXT_SECONDARY And &HFF)
            }
            pnlQueueItems.Controls.Add(emptyLabel)
        Else
            For Each queueItem In _queueItems
                queueItem.RowPanel.Location = New Point(5, yPosition)
                pnlQueueItems.Controls.Add(queueItem.RowPanel)
                yPosition += 43
            Next
        End If

        pnlQueueItems.AutoScrollMinSize = New Size(0, yPosition)
        lblQueueTitle.Text = $"Bulk Queue ({_queueItems.Count})"
        UpdateQueueStartButton()
    End Sub

    Private Sub UpdateBulkUi()
        If pnlQueue Is Nothing Then
            Return
        End If

        pnlQueue.Visible = _bulkDownloadsEnabled

        If _bulkDownloadsEnabled Then
            pnlResults.Location = New Point(25, 410)
            pnlResults.Size = New Size(950, 355)
        Else
            pnlResults.Location = New Point(25, 235)
            pnlResults.Size = New Size(950, 530)
        End If

        RefreshQueueUi()
    End Sub

    Private Sub UpdateQueueStartButton()
        If btnStartQueue Is Nothing Then
            Return
        End If

        Dim hasPendingItems = _queueItems.Any(Function(queueItem) Not queueItem.Completed)
        btnStartQueue.Enabled = _bulkDownloadsEnabled AndAlso _isInitialized AndAlso Not _isQueueRunning AndAlso hasPendingItems
        btnStartQueue.Text = If(_isQueueRunning, "Running...", "Start Queue")
    End Sub

    Private Async Sub BtnStartQueue_Click(sender As Object, e As EventArgs)
        If Not _bulkDownloadsEnabled OrElse _isQueueRunning OrElse Not _isInitialized Then
            Return
        End If

        If Not _queueItems.Any(Function(queueItem) Not queueItem.Completed) Then
            Return
        End If

        _isQueueRunning = True
        For Each queueItem In _queueItems
            queueItem.DeleteButton.Enabled = False
        Next
        UpdateQueueStartButton()

        Try
            For Each queueItem In _queueItems.ToList()
                If queueItem.Completed Then
                    Continue For
                End If

                Await ProcessQueueItemAsync(queueItem)
            Next

            lblStatusMessage.Text = "Status: Bulk queue finished."
            UpdateDebugMessage("Bulk queue finished", Color.FromArgb((SPOTIFY_GREEN >> 16) And &HFF, (SPOTIFY_GREEN >> 8) And &HFF, SPOTIFY_GREEN And &HFF))
        Finally
            _isQueueRunning = False
            For Each queueItem In _queueItems
                queueItem.DeleteButton.Enabled = True
            Next
            UpdateQueueStartButton()
        End Try
    End Sub

    Private Async Function ProcessQueueItemAsync(queueItem As QueueItem) As Task
        Try
            SetQueueItemStatus(queueItem, "Resolving Spotify...", Color.FromArgb((SPOTIFY_GREEN >> 16) And &HFF, (SPOTIFY_GREEN >> 8) And &HFF, SPOTIFY_GREEN And &HFF))
            lblStatusMessage.Text = $"Status: Resolving queue item {GetQueueItemNumber(queueItem)} of {_queueItems.Count}..."

            Dim trackInfo = SpotifyUrlParser.ExtractTrackInfo(queueItem.SpotifyUrl)
            If trackInfo Is Nothing Then
                Throw New InvalidOperationException("The Spotify URL could not be parsed.")
            End If

            Dim fullTrackInfo = Await SpotifyUrlParser.GetSpotifyTrackMetadataAsync(trackInfo.TrackId, _spotifyTrackService)
            If fullTrackInfo Is Nothing Then
                Throw New InvalidOperationException("Spotify metadata could not be retrieved.")
            End If

            Dim searchQuery = $"{fullTrackInfo.TrackName} {fullTrackInfo.ArtistName}"
            SetQueueItemStatus(queueItem, "Searching YouTube...", Color.FromArgb((SPOTIFY_GREEN >> 16) And &HFF, (SPOTIFY_GREEN >> 8) And &HFF, SPOTIFY_GREEN And &HFF))
            lblStatusMessage.Text = $"Status: Searching YouTube for: {searchQuery}"

            Dim results = Await YouTubeSearchService.SearchAsync(searchQuery)
            If results Is Nothing OrElse results.Count = 0 OrElse String.IsNullOrWhiteSpace(results(0).Url) Then
                Throw New InvalidOperationException("No YouTube results were found.")
            End If

            SetQueueItemStatus(queueItem, "Downloading first result...", Color.FromArgb((SPOTIFY_GREEN >> 16) And &HFF, (SPOTIFY_GREEN >> 8) And &HFF, SPOTIFY_GREEN And &HFF))
            lblStatusMessage.Text = $"Status: Downloading {fullTrackInfo.TrackName}..."
            Await YouTubeDownloadService.DownloadAudioAsync(results(0).Url)

            queueItem.Completed = True
            SetQueueItemStatus(queueItem, "Downloaded", Color.FromArgb((SPOTIFY_GREEN >> 16) And &HFF, (SPOTIFY_GREEN >> 8) And &HFF, SPOTIFY_GREEN And &HFF))
        Catch ex As Exception
            queueItem.Completed = False
            SetQueueItemStatus(queueItem, "Failed", Color.FromArgb(&HFF, &H66, &H66))
            lblStatusMessage.Text = $"Status: Queue item failed - {ex.Message}"
            UpdateDebugMessage($"Queue item failed: {ex.Message}", Color.Red)
        End Try
    End Function

    Private Sub SetQueueItemStatus(queueItem As QueueItem, status As String, color As Color)
        If queueItem Is Nothing OrElse queueItem.StatusLabel Is Nothing Then
            Return
        End If

        queueItem.StatusLabel.Text = status
        queueItem.StatusLabel.ForeColor = color
    End Sub

    Private Function GetQueueItemNumber(queueItem As QueueItem) As Integer
        Dim itemIndex = _queueItems.IndexOf(queueItem)
        If itemIndex < 0 Then
            Return _queueItems.Count
        End If

        Return itemIndex + 1
    End Function

    Private Async Sub BtnDownloadFirst_Click(sender As Object, e As EventArgs)
        If Not _isInitialized Then
            MessageBox.Show("Spotify is still initializing. Please wait a moment and try again.", "Not Ready", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Dim spotifyUrl = txtSpotifyUrl.Text.Trim()
        If String.IsNullOrWhiteSpace(spotifyUrl) Then
            MessageBox.Show("Please enter or drag a Spotify link", "Input Required", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Dim trackInfo = SpotifyUrlParser.ExtractTrackInfo(spotifyUrl)
        If trackInfo Is Nothing Then
            MessageBox.Show("Could not parse the Spotify URL. Please check the link.", "Parse Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return
        End If

        btnDownloadFirst.Enabled = False
        lblLoading.Visible = True
        pnlResults.Controls.Clear()
        UpdateDebugMessage("Fetching track for download...", Color.FromArgb((SPOTIFY_GREEN >> 16) And &HFF, (SPOTIFY_GREEN >> 8) And &HFF, SPOTIFY_GREEN And &HFF))
        lblStatusMessage.Text = "Status: Fetching Spotify metadata..."

        Try
            Dim fullTrackInfo = Await SpotifyUrlParser.GetSpotifyTrackMetadataAsync(trackInfo.TrackId, _spotifyTrackService)
            If fullTrackInfo Is Nothing Then
                Throw New InvalidOperationException("Could not fetch track information from Spotify API.")
            End If

            Dim searchQuery = $"{fullTrackInfo.TrackName} {fullTrackInfo.ArtistName}"
            lblStatusMessage.Text = $"Status: Searching YouTube for: {searchQuery}"
            UpdateDebugMessage("Searching YouTube for the first result...", Color.FromArgb((SPOTIFY_GREEN >> 16) And &HFF, (SPOTIFY_GREEN >> 8) And &HFF, SPOTIFY_GREEN And &HFF))

            Dim results = Await YouTubeSearchService.SearchAsync(searchQuery)
            DisplayResults(results, fullTrackInfo)

            If results Is Nothing OrElse results.Count = 0 OrElse String.IsNullOrWhiteSpace(results(0).Url) Then
                Throw New InvalidOperationException("No YouTube results were found.")
            End If

            Await DownloadResultAsync(results(0), btnDownloadFirst)
        Catch ex As Exception
            UpdateDebugMessage($"Download failed: {ex.Message}", Color.Red)
            MessageBox.Show(ex.Message, "Download Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            lblLoading.Visible = False
            If Not btnDownloadFirst.IsDisposed Then
                btnDownloadFirst.Enabled = _isInitialized
                btnDownloadFirst.Text = "DL"
            End If
        End Try
    End Sub

    Private Async Sub BtnSearch_Click(sender As Object, e As EventArgs)
        If Not _isInitialized Then
            MessageBox.Show("Spotify is still initializing. Please wait a moment and try again.", "Not Ready", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Dim spotifyUrl = txtSpotifyUrl.Text.Trim()

        If String.IsNullOrWhiteSpace(spotifyUrl) Then
            MessageBox.Show("Please enter or drag a Spotify link", "Input Required", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        ' parse spotify url
        Dim trackInfo = SpotifyUrlParser.ExtractTrackInfo(spotifyUrl)

        If trackInfo Is Nothing Then
            MessageBox.Show("Could not parse the Spotify URL. Please check the link.", "Parse Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return
        End If

        ' fetch metadata
        lblLoading.Visible = True
        UpdateDebugMessage("Fetching track from Spotify...", Color.FromArgb((SPOTIFY_GREEN >> 16) And &HFF, (SPOTIFY_GREEN >> 8) And &HFF, SPOTIFY_GREEN And &HFF))
        lblStatusMessage.Text = "Status: Fetching Spotify metadata..."
        pnlResults.Controls.Clear()

        Try
            Dim fullTrackInfo = Await SpotifyUrlParser.GetSpotifyTrackMetadataAsync(trackInfo.TrackId, _spotifyTrackService)

            If fullTrackInfo Is Nothing Then
                MessageBox.Show("Could not fetch track information from Spotify API.", "Metadata Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                lblLoading.Visible = False
                UpdateDebugMessage("Metadata fetch failed", Color.Red)
                Return
            End If

            ' build yt query
            Dim searchQuery = $"{fullTrackInfo.TrackName} {fullTrackInfo.ArtistName}"
            lblStatusMessage.Text = $"Status: Searching YouTube for: {searchQuery}"
            UpdateDebugMessage("Searching YouTube...", Color.FromArgb((SPOTIFY_GREEN >> 16) And &HFF, (SPOTIFY_GREEN >> 8) And &HFF, SPOTIFY_GREEN And &HFF))

            Dim results = Await YouTubeSearchService.SearchAsync(searchQuery)
            DisplayResults(results, fullTrackInfo)
            UpdateDebugMessage($"Found {results.Count} results", Color.FromArgb((SPOTIFY_GREEN >> 16) And &HFF, (SPOTIFY_GREEN >> 8) And &HFF, SPOTIFY_GREEN And &HFF))

        Catch ex As Exception
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            UpdateDebugMessage($"Error: {ex.Message}", Color.Red)
        Finally
            lblLoading.Visible = False
        End Try
    End Sub

    Private Sub DisplayResults(results As List(Of YouTubeSearchService.YouTubeResult), trackInfo As TrackInfo)
        pnlResults.Controls.Clear()

        If results.Count = 0 Then
            Dim noResultsLabel = New Label With {
                .Text = "No results found. Try a different search query.",
                .Font = New Font("Segoe UI", 11),
                .ForeColor = Color.FromArgb((TEXT_SECONDARY >> 16) And &HFF, (TEXT_SECONDARY >> 8) And &HFF, TEXT_SECONDARY And &HFF),
                .Dock = DockStyle.Top,
                .TextAlign = ContentAlignment.MiddleCenter,
                .Height = 50
            }
            pnlResults.Controls.Add(noResultsLabel)
            Return
        End If

        ' display track info 
        Dim headerPanel = CreateTrackInfoHeader(trackInfo)
        pnlResults.Controls.Add(headerPanel)

        Dim yPosition = 120

        For Each result In results
            Dim resultPanel = CreateResultPanel(result, yPosition)
            pnlResults.Controls.Add(resultPanel)
            yPosition += 100
        Next
    End Sub

    Private Function CreateTrackInfoHeader(trackInfo As TrackInfo) As Panel
        Dim headerPanel = New Panel With {
            .Location = New Point(10, 10),
            .Size = New Size(920, 100),
            .BackColor = Color.FromArgb((CARD_BG >> 16) And &HFF, (CARD_BG >> 8) And &HFF, CARD_BG And &HFF),
            .BorderStyle = BorderStyle.None
        }

        ' arttt
        Dim picAlbumArt = New PictureBox With {
            .Location = New Point(10, 10),
            .Size = New Size(80, 80),
            .SizeMode = PictureBoxSizeMode.StretchImage,
            .BackColor = Color.FromArgb((LIGHT_BG >> 16) And &HFF, (LIGHT_BG >> 8) And &HFF, LIGHT_BG And &HFF)
        }

        If Not String.IsNullOrEmpty(trackInfo.ImageUrl) Then
            Try
                picAlbumArt.LoadAsync(trackInfo.ImageUrl)
            Catch
                ' no worries no img
            End Try
        End If

        headerPanel.Controls.Add(picAlbumArt)

        ' trackname
        Dim lblTrackName = New Label With {
            .Text = trackInfo.TrackName,
            .Font = New Font("Segoe UI", 12, FontStyle.Bold),
            .Location = New Point(100, 10),
            .Size = New Size(810, 25),
            .ForeColor = Color.FromArgb((SPOTIFY_GREEN >> 16) And &HFF, (SPOTIFY_GREEN >> 8) And &HFF, SPOTIFY_GREEN And &HFF),
            .AutoEllipsis = True
        }
        headerPanel.Controls.Add(lblTrackName)

        ' artistname
        Dim lblArtistName = New Label With {
            .Text = trackInfo.ArtistName,
            .Font = New Font("Segoe UI", 10),
            .Location = New Point(100, 38),
            .Size = New Size(810, 20),
            .ForeColor = Color.FromArgb((TEXT_PRIMARY >> 16) And &HFF, (TEXT_PRIMARY >> 8) And &HFF, TEXT_PRIMARY And &HFF),
            .AutoEllipsis = True
        }
        headerPanel.Controls.Add(lblArtistName)

        ' albumname
        Dim lblAlbumName = New Label With {
            .Text = trackInfo.AlbumName,
            .Font = New Font("Segoe UI", 9),
            .Location = New Point(100, 60),
            .Size = New Size(810, 20),
            .ForeColor = Color.FromArgb((TEXT_SECONDARY >> 16) And &HFF, (TEXT_SECONDARY >> 8) And &HFF, TEXT_SECONDARY And &HFF),
            .AutoEllipsis = True
        }
        headerPanel.Controls.Add(lblAlbumName)

        Return headerPanel
    End Function

    Private Function CreateResultPanel(result As YouTubeSearchService.YouTubeResult, yPos As Integer) As Panel
        Dim resultPanel = New Panel With {
            .Location = New Point(10, yPos),
            .Size = New Size(920, 90),
            .BackColor = Color.FromArgb((CARD_BG >> 16) And &HFF, (CARD_BG >> 8) And &HFF, CARD_BG And &HFF),
            .BorderStyle = BorderStyle.None,
            .Cursor = Cursors.Hand
        }

        ' thumbnail
        Dim picThumbnail = New PictureBox With {
            .Location = New Point(10, 10),
            .Size = New Size(60, 60),
            .SizeMode = PictureBoxSizeMode.StretchImage,
            .BackColor = Color.FromArgb((LIGHT_BG >> 16) And &HFF, (LIGHT_BG >> 8) And &HFF, LIGHT_BG And &HFF)
        }

        Try
            picThumbnail.LoadAsync(result.ThumbnailUrl)
        Catch
            ' ignore on err
        End Try

        resultPanel.Controls.Add(picThumbnail)

        ' Ttle label
        Dim lblResultTitle = New Label With {
            .Text = result.Title,
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .Location = New Point(80, 10),
            .Size = New Size(560, 35),
            .ForeColor = Color.FromArgb((TEXT_PRIMARY >> 16) And &HFF, (TEXT_PRIMARY >> 8) And &HFF, TEXT_PRIMARY And &HFF),
            .AutoEllipsis = True
        }
        resultPanel.Controls.Add(lblResultTitle)

        ' Channel Label
        Dim lblChannel = New Label With {
            .Text = If(String.IsNullOrWhiteSpace(result.Channel), "YouTube Video", result.Channel),
            .Font = New Font("Segoe UI", 9),
            .Location = New Point(80, 50),
            .Size = New Size(560, 20),
            .ForeColor = Color.FromArgb((TEXT_SECONDARY >> 16) And &HFF, (TEXT_SECONDARY >> 8) And &HFF, TEXT_SECONDARY And &HFF)
        }

        If Not String.IsNullOrWhiteSpace(result.Duration) Then
            lblChannel.Text = $"{lblChannel.Text} • {result.Duration}"
        End If

        resultPanel.Controls.Add(lblChannel)

        ' copy da url (for my vrchat peeps ykyk)
        Dim btnCopyUrl = New Button With {
            .Text = "Copy",
            .Location = New Point(680, 15),
            .Size = New Size(60, 30),
            .Font = New Font("Segoe UI", 9),
            .BackColor = Color.FromArgb(&H404040),
            .ForeColor = Color.FromArgb((TEXT_PRIMARY >> 16) And &HFF, (TEXT_PRIMARY >> 8) And &HFF, TEXT_PRIMARY And &HFF),
            .FlatStyle = FlatStyle.Flat,
            .Tag = result.Url
        }
        btnCopyUrl.FlatAppearance.BorderSize = 1
        btnCopyUrl.FlatAppearance.BorderColor = Color.FromArgb(&H666666)

        AddHandler btnCopyUrl.Click, Sub(sender As Object, e As EventArgs)
                                         Clipboard.SetText(result.Url)
                                         MessageBox.Show("URL copied to clipboard!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
                                     End Sub

        resultPanel.Controls.Add(btnCopyUrl)

        ' open url
        Dim btnOpen = New Button With {
            .Text = "Play",
            .Location = New Point(750, 15),
            .Size = New Size(60, 30),
            .Font = New Font("Segoe UI", 9, FontStyle.Bold),
            .BackColor = Color.FromArgb((YOUTUBE_RED >> 16) And &HFF, (YOUTUBE_RED >> 8) And &HFF, YOUTUBE_RED And &HFF),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Tag = result.Url
        }
        btnOpen.FlatAppearance.BorderSize = 0

        AddHandler btnOpen.Click, Sub(sender As Object, e As EventArgs)
                                      YouTubeSearchService.OpenInBrowser(result.Url)
                                  End Sub

        resultPanel.Controls.Add(btnOpen)

        Dim btnDownload = New Button With {
            .Text = "Download",
            .Location = New Point(815, 15),
            .Size = New Size(90, 30),
            .Font = New Font("Segoe UI", 9, FontStyle.Bold),
            .BackColor = Color.FromArgb((SPOTIFY_GREEN >> 16) And &HFF, (SPOTIFY_GREEN >> 8) And &HFF, SPOTIFY_GREEN And &HFF),
            .ForeColor = Color.Black,
            .FlatStyle = FlatStyle.Flat
        }
        btnDownload.FlatAppearance.BorderSize = 0

        AddHandler btnDownload.Click, Async Sub(sender As Object, e As EventArgs)
                                             Await DownloadResultAsync(result, btnDownload)
                                         End Sub

        resultPanel.Controls.Add(btnDownload)

        ' click handla for the wholle thing
        AddHandler resultPanel.Click, Sub(sender As Object, e As EventArgs)
                                          YouTubeSearchService.OpenInBrowser(result.Url)
                                      End Sub

        Return resultPanel
    End Function

    Private Async Function DownloadResultAsync(result As YouTubeSearchService.YouTubeResult, downloadButton As Button) As Task
        Dim originalText = downloadButton.Text
        downloadButton.Enabled = False
        downloadButton.Text = "Saving..."
        lblStatusMessage.Text = $"Status: Downloading {result.Title}..."
        UpdateDebugMessage("Downloading audio...", Color.FromArgb((SPOTIFY_GREEN >> 16) And &HFF, (SPOTIFY_GREEN >> 8) And &HFF, SPOTIFY_GREEN And &HFF))

        Try
            Dim downloadedFile = Await YouTubeDownloadService.DownloadAudioAsync(result.Url)
            lblStatusMessage.Text = $"Status: Saved to {downloadedFile}"
            UpdateDebugMessage("Download complete", Color.FromArgb((SPOTIFY_GREEN >> 16) And &HFF, (SPOTIFY_GREEN >> 8) And &HFF, SPOTIFY_GREEN And &HFF))
            MessageBox.Show($"The song was downloaded to:{Environment.NewLine}{downloadedFile}", "Download Complete", MessageBoxButtons.OK, MessageBoxIcon.Information)
        Catch ex As Exception
            lblStatusMessage.Text = "Status: Download failed"
            UpdateDebugMessage($"Download failed: {ex.Message}", Color.Red)
            MessageBox.Show(ex.Message, "Download Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            If Not downloadButton.IsDisposed Then
                downloadButton.Enabled = True
                downloadButton.Text = originalText
            End If
        End Try
    End Function
End Class
