Imports System.Net.Http
Imports System.Text
Imports System.Text.RegularExpressions
Imports Newtonsoft.Json.Linq

Public Class YouTubeSearchService
    Private Shared ReadOnly client As New HttpClient()

    Public Class YouTubeResult
        Public Property Title As String
        Public Property Url As String
        Public Property VideoId As String
        Public Property ThumbnailUrl As String
        Public Property Duration As String
        Public Property Channel As String
    End Class

    Public Shared Async Function SearchAsync(query As String) As Task(Of List(Of YouTubeResult))
        If String.IsNullOrWhiteSpace(query) Then
            Return New List(Of YouTubeResult)()
        End If

        Try
            Dim searchUrl = $"https://www.youtube.com/results?search_query={Uri.EscapeDataString(query)}"

            ' Setting da user agent to mimic a browser so yt doesnt YEET your requests.
            client.DefaultRequestHeaders.Clear()
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36")

            Dim response = Await client.GetAsync(searchUrl)
            response.EnsureSuccessStatusCode()

            Dim content = Await response.Content.ReadAsStringAsync()

            Return ParseSearchResults(content)

        Catch ex As Exception
            Console.WriteLine($"Search error: {ex.Message}")
            Return New List(Of YouTubeResult)()
        End Try
    End Function

    Private Shared Function ParseSearchResults(html As String) As List(Of YouTubeResult)
        Dim results = New List(Of YouTubeResult)()

        Try
            Dim jsonData = ExtractInitialDataJson(html)
            If Not String.IsNullOrEmpty(jsonData) Then
                results = ExtractVideosFromJson(jsonData)
            End If

        Catch ex As Exception
            Console.WriteLine($"Parse error: {ex.Message}")
        End Try

        Return results.Take(10).ToList() ' rtn top 10
    End Function

    Private Shared Function ExtractInitialDataJson(html As String) As String
        Dim marker = "ytInitialData"
        Dim markerIndex = html.IndexOf(marker, StringComparison.Ordinal)

        If markerIndex < 0 Then
            Return String.Empty
        End If

        Dim objectStart = html.IndexOf("{"c, markerIndex)
        If objectStart < 0 Then
            Return String.Empty
        End If

        Dim depth = 0
        Dim inString = False
        Dim isEscaped = False

        For index As Integer = objectStart To html.Length - 1
            Dim character = html(index)

            If inString Then
                If isEscaped Then
                    isEscaped = False
                ElseIf character = "\"c Then
                    isEscaped = True
                ElseIf character = """"c Then
                    inString = False
                End If
            ElseIf character = """"c Then
                inString = True
            ElseIf character = "{"c Then
                depth += 1
            ElseIf character = "}"c Then
                depth -= 1
                If depth = 0 Then
                    Return html.Substring(objectStart, index - objectStart + 1)
                End If
            End If
        Next

        Return String.Empty
    End Function

    Private Shared Function ExtractVideosFromJson(json As String) As List(Of YouTubeResult)
        Dim results = New List(Of YouTubeResult)()
        Dim foundVideoIds = New HashSet(Of String)()

        Try
            Dim jsonData = JObject.Parse(json)

            For Each rendererToken As JToken In jsonData.SelectTokens("$..videoRenderer")
                Dim renderer = TryCast(rendererToken, JObject)
                If renderer Is Nothing Then
                    Continue For
                End If

                Dim videoId = GetTokenText(renderer("videoId"))
                Dim title = GetNestedText(renderer("title"))

                If Not Regex.IsMatch(videoId, "^[a-zA-Z0-9_-]{11}$") OrElse String.IsNullOrWhiteSpace(title) Then
                    Continue For
                End If

                If foundVideoIds.Contains(videoId) Then
                    Continue For
                End If

                foundVideoIds.Add(videoId)

                Dim channel = GetNestedText(renderer("longBylineText"))
                If String.IsNullOrWhiteSpace(channel) Then
                    channel = GetNestedText(renderer("ownerText"))
                End If
                If String.IsNullOrWhiteSpace(channel) Then
                    channel = GetNestedText(renderer("shortBylineText"))
                End If

                results.Add(New YouTubeResult With {
                    .VideoId = videoId,
                    .Title = DecodeHtmlEntities(title),
                    .Url = $"https://www.youtube.com/watch?v={videoId}",
                    .ThumbnailUrl = $"https://img.youtube.com/vi/{videoId}/default.jpg",
                    .Duration = GetNestedText(renderer("lengthText")),
                    .Channel = DecodeHtmlEntities(channel)
                })

                If results.Count >= 10 Then Exit For
            Next

        Catch ex As Exception
            Console.WriteLine($"JSON extraction error: {ex.Message}")
        End Try

        Return results
    End Function

    Private Shared Function GetNestedText(token As JToken) As String
        If token Is Nothing Then
            Return String.Empty
        End If

        Dim simpleText = token("simpleText")
        If simpleText IsNot Nothing Then
            Return GetTokenText(simpleText)
        End If

        Dim runs = TryCast(token("runs"), JArray)
        If runs Is Nothing Then
            Return String.Empty
        End If

        Dim text = New StringBuilder()
        For Each run As JToken In runs
            text.Append(GetTokenText(run("text")))
        Next

        Return text.ToString()
    End Function

    Private Shared Function GetTokenText(token As JToken) As String
        If token Is Nothing Then
            Return String.Empty
        End If

        Return token.ToString()
    End Function

    Private Shared Function DecodeHtmlEntities(text As String) As String
        If String.IsNullOrEmpty(text) Then
            Return text
        End If

        ' Decode html
        Return System.Net.WebUtility.HtmlDecode(text)
    End Function

    Public Shared Function GetYouTubeUrl(videoId As String) As String
        Return $"https://www.youtube.com/watch?v={videoId}"
    End Function

    Public Shared Sub OpenInBrowser(url As String)
        Try
            Dim psi = New System.Diagnostics.ProcessStartInfo With {
                .FileName = url,
                .UseShellExecute = True
            }
            System.Diagnostics.Process.Start(psi)
        Catch ex As Exception
            Console.WriteLine($"Error opening browser: {ex.Message}")
        End Try
    End Sub
End Class
