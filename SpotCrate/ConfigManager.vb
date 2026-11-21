Imports System.IO
Imports Newtonsoft.Json

Public Class ConfigManager
    Private Shared ReadOnly ConfigFileName As String = "spotifyconfig.json"
    Private Shared ReadOnly ConfigPath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFileName)

    Public Class SpotifyCredentials
        <JsonProperty("clientId")>
        Public Property ClientId As String

        <JsonProperty("clientSecret")>
        Public Property ClientSecret As String

        <JsonProperty("lastUpdated")>
        Public Property LastUpdated As DateTime

        <JsonProperty("downloadDirectory")>
        Public Property DownloadDirectory As String

        <JsonProperty("bulkDownloadsEnabled")>
        Public Property BulkDownloadsEnabled As Boolean
    End Class

    Public Shared Sub Initialize()
        If Not File.Exists(ConfigPath) Then
            ' Create default config with empty creds
            Dim defaultConfig = New SpotifyCredentials With {
                .ClientId = "",
                .ClientSecret = "",
                .LastUpdated = DateTime.Now,
                .DownloadDirectory = GetDefaultDownloadDirectory(),
                .BulkDownloadsEnabled = False
            }
            SaveConfig(defaultConfig)
        End If
    End Sub

    Public Shared Sub SaveBulkDownloadsEnabled(enabled As Boolean)
        Try
            Dim credentials = LoadCredentials()
            credentials.BulkDownloadsEnabled = enabled
            credentials.LastUpdated = DateTime.Now
            SaveConfig(credentials)
        Catch ex As Exception
            Throw New Exception($"Failed to save bulk download setting: {ex.Message}", ex)
        End Try
    End Sub

    Public Shared Function LoadCredentials() As SpotifyCredentials
        Try
            If Not File.Exists(ConfigPath) Then
                Initialize()
                Return New SpotifyCredentials()
            End If

            Dim json = File.ReadAllText(ConfigPath)
            Dim credentials = JsonConvert.DeserializeObject(Of SpotifyCredentials)(json)
            Return If(credentials, New SpotifyCredentials())
        Catch ex As Exception
            Console.WriteLine($"Error loading config: {ex.Message}")
            Return New SpotifyCredentials()
        End Try
    End Function

    Public Shared Sub SaveCredentials(clientId As String, clientSecret As String)
        Try
            Dim credentials = LoadCredentials()
            credentials.ClientId = clientId
            credentials.ClientSecret = clientSecret
            credentials.LastUpdated = DateTime.Now
            SaveConfig(credentials)
        Catch ex As Exception
            Throw New Exception($"Failed to save credentials: {ex.Message}", ex)
        End Try
    End Sub

    Public Shared Sub SaveDownloadDirectory(downloadDirectory As String)
        If String.IsNullOrWhiteSpace(downloadDirectory) Then
            Throw New ArgumentException("A download directory is required.", NameOf(downloadDirectory))
        End If

        Try
            Dim credentials = LoadCredentials()
            credentials.DownloadDirectory = Path.GetFullPath(downloadDirectory.Trim())
            credentials.LastUpdated = DateTime.Now
            SaveConfig(credentials)
        Catch ex As Exception
            Throw New Exception($"Failed to save download directory: {ex.Message}", ex)
        End Try
    End Sub

    Public Shared Sub ClearCredentials()
        Try
            Dim credentials = LoadCredentials()
            credentials.ClientId = String.Empty
            credentials.ClientSecret = String.Empty
            credentials.LastUpdated = DateTime.Now
            SaveConfig(credentials)
        Catch ex As Exception
            Throw New Exception($"Failed to clear Spotify credentials: {ex.Message}", ex)
        End Try
    End Sub

    Public Shared Function GetDownloadDirectory() As String
        Dim credentials = LoadCredentials()
        If Not String.IsNullOrWhiteSpace(credentials.DownloadDirectory) Then
            Return credentials.DownloadDirectory
        End If

        Return GetDefaultDownloadDirectory()
    End Function

    Public Shared Function GetBulkDownloadsEnabled() As Boolean
        Return LoadCredentials().BulkDownloadsEnabled
    End Function

    Public Shared Function GetDefaultDownloadDirectory() As String
        Return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), "SpotCrate")
    End Function

    Private Shared Sub SaveConfig(credentials As SpotifyCredentials)
        Try
            Dim json = JsonConvert.SerializeObject(credentials, Formatting.Indented)
            File.WriteAllText(ConfigPath, json)
        Catch ex As Exception
            Throw New Exception($"Failed to write config file: {ex.Message}", ex)
        End Try
    End Sub

    Public Shared Function AreCredentialsConfigured() As Boolean
        Dim creds = LoadCredentials()
        Return Not String.IsNullOrEmpty(creds.ClientId) AndAlso Not String.IsNullOrEmpty(creds.ClientSecret)
    End Function

    Public Shared Function GetConfigPath() As String
        Return ConfigPath
    End Function
End Class
